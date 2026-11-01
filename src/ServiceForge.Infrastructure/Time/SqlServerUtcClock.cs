using System.Data;
using System.Data.Common;

namespace ServiceForge.Infrastructure.Time;

public sealed class SqlServerUtcClock : ISqlServerUtcClock
{
    public async Task<DateTime> GetUtcNowAsync(
        DbConnection connection,
        DbTransaction? transaction = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SYSUTCDATETIME();";
        command.CommandType = CommandType.Text;
        command.Transaction = transaction;
        var value = await command.ExecuteScalarAsync(cancellationToken);
        if (value is not DateTime utcNow)
        {
            throw new InvalidOperationException("SQL Server did not return a UTC datetime value.");
        }

        return DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
    }
}
