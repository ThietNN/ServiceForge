using System.Data.Common;

namespace ServiceForge.Infrastructure.Time;

public interface ISqlServerUtcClock
{
    Task<DateTime> GetUtcNowAsync(
        DbConnection connection,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
