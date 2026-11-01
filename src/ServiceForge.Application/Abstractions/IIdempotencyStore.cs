using ServiceForge.Application.Appointments.Idempotency;

namespace ServiceForge.Application.Abstractions;

public interface IIdempotencyStore
{
    Task<IdempotencyLookupResult> LookupAsync(
        string key,
        ReadOnlyMemory<byte> requestHash,
        DateTime authoritativeUtcNow,
        CancellationToken cancellationToken);
}
