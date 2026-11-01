using System.Data.Common;

namespace ServiceForge.Application.Abstractions;

public interface ISqlServerUtcClock
{
    Task<DateTime> GetUtcNowAsync(
        DbConnection connection,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default);
}
