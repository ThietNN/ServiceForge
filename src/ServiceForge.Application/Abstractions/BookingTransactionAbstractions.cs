using System.Data.Common;

namespace ServiceForge.Application.Abstractions;

public interface IBookingTransaction : IAsyncDisposable
{
    DbTransaction DbTransaction { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public interface IBookingTransactionFactory
{
    Task<IBookingTransaction> BeginAsync(CancellationToken cancellationToken = default);
}
