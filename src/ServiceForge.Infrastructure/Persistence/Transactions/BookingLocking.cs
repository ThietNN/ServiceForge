using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using ServiceForge.Application.Abstractions;

namespace ServiceForge.Infrastructure.Persistence.Transactions;

public static class BookingLockResourceNames
{
    public static string For(BookingLockKind kind, string value) =>
        kind switch
        {
            BookingLockKind.IdempotencyKey => $"ServiceForge:Booking:IdempotencyKey:{value}",
            BookingLockKind.Vehicle => $"ServiceForge:Booking:Vehicle:{value}",
            BookingLockKind.Dealership => $"ServiceForge:Booking:Dealership:{value}",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
}

public sealed class SqlServerBookingLockManager : IBookingLockManager
{
    public async Task AcquireAsync(
        DbTransaction transaction,
        BookingLockKind kind,
        string value,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var resourceName = BookingLockResourceNames.For(kind, value);
        await using var command = transaction.Connection?.CreateCommand()
            ?? throw new InvalidOperationException("The transaction has no active connection.");
        command.Transaction = transaction;
        command.CommandTimeout = 6;
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 5000,
                @DbPrincipal = 'public';
            SELECT @result;
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@resource";
        parameter.DbType = DbType.String;
        parameter.Size = 255;
        parameter.Value = resourceName;
        command.Parameters.Add(parameter);

        object? result;
        try
        {
            result = await command.ExecuteScalarAsync(cancellationToken);
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            throw new BookingTransientFailureException("SQL Server reported a booking deadlock victim.", exception);
        }
        var returnCode = Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
        if (returnCode < 0)
        {
            throw new BookingLockException(returnCode, resourceName);
        }
    }
}
