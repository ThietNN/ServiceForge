using System.Text.Json;
using FluentValidation;
using MediatR;
using ServiceForge.Application.Abstractions;
using ServiceForge.Application.Appointments.Allocation;
using ServiceForge.Application.Appointments.Contracts;
using ServiceForge.Application.Appointments.Idempotency;
using ServiceForge.Application.Appointments.Rules;
using ServiceForge.Application.Validation;
using ServiceForge.Domain.Entities;
using ServiceForge.Domain.Enums;
using ServiceForge.Domain.Time;

namespace ServiceForge.Application.Appointments.CreateAppointment;

public sealed class CreateAppointmentCommandHandler(
    IValidator<CreateAppointmentCommand> validator,
    IBookingTransactionFactory transactionFactory,
    IBookingLockManager lockManager,
    IIdempotencyStore idempotencyStore,
    IBookingReferenceDataQueries referenceDataQueries,
    IVehicleBookingQueries vehicleBookingQueries,
    IBookingResourceQueries resourceQueries,
    IAtomicBookingPersistence bookingPersistence,
    ISqlServerUtcClock sqlServerUtcClock) : IRequestHandler<CreateAppointmentCommand, BookingOutcome>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<BookingOutcome> Handle(CreateAppointmentCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key.Replace("Request.", string.Empty), group => group.Select(error => error.ErrorMessage).ToArray());
            return errors.ContainsKey("StartTime")
                ? BookingOutcome.InvalidBookingTime(errors)
                : BookingOutcome.Invalid(errors);
        }

        var request = command.Request;
        var preliminaryTime = ParseRequestTime(request.StartTime);
        if (preliminaryTime is null)
        {
            return BookingOutcome.Failure(400, BookingRuleErrorCodes.InvalidBookingTime,
                "Start time must be a valid ISO 8601 timestamp with an explicit UTC offset.");
        }

        await using var transaction = await transactionFactory.BeginAsync(cancellationToken);
        var transactionCompleted = false;
        try
        {
            try
            {
                await lockManager.AcquireAsync(
                    transaction.DbTransaction,
                    BookingLockKind.IdempotencyKey,
                    command.IdempotencyKey,
                    cancellationToken);
            }
            catch (BookingLockException exception) when (exception.ReturnCode == -1)
            {
                return BookingOutcome.Failure(409, "REQUEST_IN_PROGRESS",
                    "Another booking request using this idempotency key is in progress.");
            }
            catch (BookingTransientFailureException)
            {
                return BookingOutcome.Failure(503, "BOOKING_TEMPORARILY_UNAVAILABLE",
                    "The booking could not be serialized because of a transient database failure.");
            }
            catch (BookingLockException)
            {
                return BookingOutcome.Failure(503, "BOOKING_TEMPORARILY_UNAVAILABLE",
                    "The booking could not acquire the idempotency lock.");
            }

            var idempotencyNow = await sqlServerUtcClock.GetUtcNowAsync(
                transaction.DbTransaction.Connection!, transaction.DbTransaction, cancellationToken);
            var identity = new CanonicalRequestIdentity(
                request.CustomerId,
                request.VehicleId,
                request.DealershipId,
                request.ServiceTypeId,
                preliminaryTime.Value.UtcDateTime);
            var requestHash = CanonicalRequestHasher.ComputeHash(identity);
            var lookup = await idempotencyStore.LookupAsync(
                command.IdempotencyKey, requestHash, idempotencyNow, cancellationToken);

            if (lookup.Status == IdempotencyLookupStatus.MatchingUnexpired)
            {
                var replay = JsonSerializer.Deserialize<AppointmentResponse>(
                    lookup.Record!.ResponseBody, JsonOptions)
                    ?? throw new InvalidOperationException("Stored idempotency response could not be read.");
                transactionCompleted = true;
                await transaction.CommitAsync(cancellationToken);
                return BookingOutcome.Success(replay, lookup.Location!, true);
            }

            if (lookup.Status == IdempotencyLookupStatus.ConflictingUnexpired)
            {
                transactionCompleted = true;
                await transaction.CommitAsync(cancellationToken);
                return BookingOutcome.Failure(409, "IDEMPOTENCY_KEY_CONFLICT",
                    "The supplied Idempotency-Key was already used for a different request.");
            }

            try
            {
                await lockManager.AcquireAsync(transaction.DbTransaction, BookingLockKind.Vehicle,
                    request.VehicleId.ToString(System.Globalization.CultureInfo.InvariantCulture), cancellationToken);
                await lockManager.AcquireAsync(transaction.DbTransaction, BookingLockKind.Dealership,
                    request.DealershipId.ToString(System.Globalization.CultureInfo.InvariantCulture), cancellationToken);
            }
            catch (BookingLockException)
            {
                return BookingOutcome.Failure(503, "BOOKING_TEMPORARILY_UNAVAILABLE",
                    "The booking could not acquire the required resource locks.");
            }
            catch (BookingTransientFailureException)
            {
                return BookingOutcome.Failure(503, "BOOKING_TEMPORARILY_UNAVAILABLE",
                    "The booking could not be serialized because of a transient database failure.");
            }

            var authoritativeNow = await sqlServerUtcClock.GetUtcNowAsync(
                transaction.DbTransaction.Connection!, transaction.DbTransaction, cancellationToken);
            var referenceData = await referenceDataQueries.GetAsync(
                request.CustomerId, request.VehicleId, request.DealershipId, request.ServiceTypeId, cancellationToken);
            if (!referenceData.CustomerExists || !referenceData.VehicleExists || !referenceData.DealershipExists
                || !referenceData.ServiceTypeExists)
            {
                return BookingOutcome.Failure(404, "RESOURCE_NOT_FOUND", "One or more booking resources were not found.");
            }

            if (referenceData.VehicleOwnerCustomerId != request.CustomerId)
            {
                return BookingOutcome.Failure(403, BookingRuleErrorCodes.VehicleNotOwned,
                    "The vehicle is not owned by the booking customer.");
            }

            var bookingClock = new FixedClock(authoritativeNow);
            var normalized = new BookingTimeValidator(bookingClock).Validate(
                request.StartTime, referenceData.ServiceDurationMinutes!.Value);
            if (!normalized.IsValid)
            {
                return BookingOutcome.Failure(400, normalized.ErrorCode!, normalized.ErrorMessage!);
            }

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(referenceData.DealershipTimeZoneId!);
            var localWindow = new DealershipLocalTimeValidator(bookingClock).Validate(normalized.Value!.Interval, timeZone);
            if (!localWindow.IsValid)
            {
                return BookingOutcome.Failure(400, localWindow.ErrorCode!, localWindow.ErrorMessage!);
            }

            if (await vehicleBookingQueries.HasOccupiedOverlapAsync(
                    request.VehicleId, normalized.Value.Interval, cancellationToken))
            {
                return BookingOutcome.Failure(409, BookingRuleErrorCodes.VehicleTimeConflict,
                    "The vehicle already has an overlapping appointment.");
            }

            var eligibility = await resourceQueries.GetEligibilityAsync(
                request.DealershipId, request.ServiceTypeId, normalized.Value.Interval, cancellationToken);
            var allocation = ResourceAllocator.SelectLowestAvailable(eligibility);
            if (!allocation.IsValid)
            {
                return BookingOutcome.Failure(409, allocation.ErrorCode!, allocation.ErrorMessage!);
            }

            var appointment = new Appointment(
                0,
                request.CustomerId,
                request.VehicleId,
                request.DealershipId,
                request.ServiceTypeId,
                allocation.Value!.TechnicianId,
                allocation.Value.ServiceBayId,
                normalized.Value.Interval.StartUtc,
                normalized.Value.Interval.EndUtc,
                normalized.Value.DurationMinutes,
                AppointmentStatus.Confirmed,
                AssignmentStatus.Assigned,
                null,
                authoritativeNow);
            var response = new AppointmentResponse(
                0, appointment.Status, appointment.AssignmentStatus, appointment.CustomerId,
                appointment.VehicleId, appointment.DealershipId, appointment.ServiceTypeId,
                appointment.TechnicianId, appointment.ServiceBayId, appointment.ScheduledStartUtc,
                appointment.ScheduledEndUtc, appointment.DurationMinutes);

            await bookingPersistence.PersistAsync(
                appointment,
                command.IdempotencyKey,
                requestHash,
                201,
                id => JsonSerializer.Serialize(response with { Id = id }, JsonOptions),
                authoritativeNow,
                authoritativeNow.AddHours(24),
                cancellationToken);
            transactionCompleted = true;
            await transaction.CommitAsync(cancellationToken);
            return BookingOutcome.Success(response with { Id = appointment.Id }, $"/api/appointments/{appointment.Id}");
        }
        catch (BookingTransientFailureException)
        {
            return BookingOutcome.Failure(503, "BOOKING_TEMPORARILY_UNAVAILABLE",
                "The booking could not be completed because of a transient database failure.");
        }
        finally
        {
            if (!transactionCompleted)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
        }
    }

    private static DateTimeOffset? ParseRequestTime(string value) =>
        Iso8601TimestampParser.TryParse(value, out var parsed) ? parsed : null;

    private sealed class FixedClock(DateTime utcNow) : IAuthoritativeUtcClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
