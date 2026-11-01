using System.Data.Common;

namespace ServiceForge.Application.Abstractions;

public enum BookingLockKind
{
    IdempotencyKey,
    Vehicle,
    Dealership
}

public sealed class BookingLockException(int returnCode, string resourceName)
    : InvalidOperationException($"SQL Server application lock failed with return code {returnCode}.")
{
    public int ReturnCode { get; } = returnCode;
    public string ResourceName { get; } = resourceName;
}

public interface IBookingLockManager
{
    Task AcquireAsync(
        DbTransaction transaction,
        BookingLockKind kind,
        string value,
        CancellationToken cancellationToken = default);
}
