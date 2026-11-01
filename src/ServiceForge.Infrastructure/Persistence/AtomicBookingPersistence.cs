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
        IdempotencyRecordData idempotencyRecord,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(appointment);
        ArgumentNullException.ThrowIfNull(idempotencyRecord);

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
            .SingleOrDefaultAsync(item => item.Key == idempotencyRecord.Key, cancellationToken);
        if (existing is not null)
        {
            if (existing.ExpiresAtUtc > idempotencyRecord.CreatedAtUtc)
            {
                throw new InvalidOperationException("An unexpired idempotency record already exists.");
            }

            dbContext.IdempotencyRecords.Remove(existing);
        }

        dbContext.IdempotencyRecords.Add(new IdempotencyRecord(
            idempotencyRecord.Key,
            idempotencyRecord.RequestHash,
            appointment.Id,
            idempotencyRecord.ResponseStatusCode,
            idempotencyRecord.ResponseBody,
            idempotencyRecord.CreatedAtUtc,
            idempotencyRecord.ExpiresAtUtc));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
