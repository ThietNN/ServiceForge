using Microsoft.EntityFrameworkCore;
using ServiceForge.Application.Abstractions;
using ServiceForge.Application.Appointments.Idempotency;

namespace ServiceForge.Infrastructure.Persistence.Idempotency;

public sealed class SqlServerIdempotencyStore(Persistence.ServiceForgeDbContext dbContext) : IIdempotencyStore
{
    public async Task<IdempotencyLookupResult> LookupAsync(
        string key,
        ReadOnlyMemory<byte> requestHash,
        DateTime authoritativeUtcNow,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var record = await dbContext.IdempotencyRecords.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Key == key, cancellationToken);

        if (record is null)
        {
            return new IdempotencyLookupResult(IdempotencyLookupStatus.Missing, null);
        }

        if (record.ExpiresAtUtc <= authoritativeUtcNow)
        {
            return new IdempotencyLookupResult(IdempotencyLookupStatus.Expired, record);
        }

        return requestHash.Span.SequenceEqual(record.RequestHash)
            ? new IdempotencyLookupResult(IdempotencyLookupStatus.MatchingUnexpired, record)
            : new IdempotencyLookupResult(IdempotencyLookupStatus.ConflictingUnexpired, record);
    }
}
