using ServiceForge.Application.Appointments.Idempotency;
using ServiceForge.Domain.Entities;

namespace ServiceForge.Application.Abstractions;

public interface IAtomicBookingPersistence
{
    Task PersistAsync(
        Appointment appointment,
        IdempotencyRecordData idempotencyRecord,
        CancellationToken cancellationToken);
}
