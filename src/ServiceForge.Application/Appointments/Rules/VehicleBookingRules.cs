using ServiceForge.Domain.Entities;
using ServiceForge.Domain.Enums;
using ServiceForge.Domain.Time;

namespace ServiceForge.Application.Appointments.Rules;

public static class VehicleBookingRules
{
    public static RuleResult<bool> ValidateOwnership(Vehicle vehicle, long customerId)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        return vehicle.CustomerId == customerId
            ? RuleResult<bool>.Success(true)
            : RuleResult<bool>.Failure(
                BookingRuleErrorCodes.VehicleNotOwned,
                "The vehicle is not owned by the booking customer.");
    }

    public static bool HasOccupiedOverlap(
        IEnumerable<Appointment> appointments,
        long vehicleId,
        BookingInterval requestedInterval)
    {
        return appointments.Any(appointment =>
            appointment.VehicleId == vehicleId
            && appointment.Status != AppointmentStatus.Cancelled
            && requestedInterval.Overlaps(BookingInterval.Create(
                appointment.ScheduledStartUtc,
                appointment.ScheduledEndUtc)));
    }
}
