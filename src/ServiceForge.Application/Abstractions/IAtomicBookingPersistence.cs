using ServiceForge.Application.Appointments.Idempotency;
using ServiceForge.Domain.Entities;

namespace ServiceForge.Application.Abstractions;

public interface IAtomicBookingPersistence
{
    Task PersistAsync(
        Appointment appointment,
        string key,
        ReadOnlyMemory<byte> requestHash,
        short responseStatusCode,
        Func<long, string> responseBodyFactory,
        DateTime createdAtUtc,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken);
}
