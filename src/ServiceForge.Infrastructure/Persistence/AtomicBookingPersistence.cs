using Microsoft.EntityFrameworkCore;
using ServiceForge.Application.Abstractions;
using ServiceForge.Application.Appointments.Idempotency;
using ServiceForge.Domain.Entities;

namespace ServiceForge.Infrastructure.Persistence;

public sealed class SqlServerAtomicBookingPersistence(
    ServiceForgeDbContext dbContext) : IAtomicBookingPersistence
{
    public async Task PersistAsync(
        Appointment appointment,
        string key,
        ReadOnlyMemory<byte> requestHash,
        short responseStatusCode,
        Func<long, string> responseBodyFactory,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(responseBodyFactory);

        if (dbContext.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Atomic booking persistence requires an active SQL Server transaction.");
        }

        if (appointment.Status != Domain.Enums.AppointmentStatus.Confirmed
            || appointment.AssignmentStatus != Domain.Enums.AssignmentStatus.Assigned
            || appointment.TechnicianId is null
            || appointment.ServiceBayId is null)
        {
            throw new ArgumentException("P0 atomic booking persistence requires a Confirmed and Assigned appointment.", nameof(appointment));
        }

        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var existing = await dbContext.IdempotencyRecords
            .SingleOrDefaultAsync(item => item.Key == key, cancellationToken);
        if (existing is not null)
        {
            if (existing.ExpiresAtUtc > createdAtUtc)
            {
                throw new InvalidOperationException("An unexpired idempotency record already exists.");
            }

            dbContext.IdempotencyRecords.Remove(existing);
        }

        dbContext.IdempotencyRecords.Add(new IdempotencyRecord(
            key,
            requestHash.ToArray(),
            appointment.Id,
            responseStatusCode,
            responseBodyFactory(appointment.Id),
            createdAtUtc,
            expiresAtUtc));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
