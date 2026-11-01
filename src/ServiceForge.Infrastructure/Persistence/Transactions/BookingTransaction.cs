using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ServiceForge.Application.Abstractions;

namespace ServiceForge.Infrastructure.Persistence.Transactions;

public sealed class SqlServerBookingTransaction : IBookingTransaction
{
    private readonly IDbContextTransaction transaction;

    internal SqlServerBookingTransaction(IDbContextTransaction transaction)
    {
        this.transaction = transaction;
    }

    public System.Data.Common.DbTransaction DbTransaction => transaction.GetDbTransaction();

    public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);

    public Task RollbackAsync(CancellationToken cancellationToken = default) => transaction.RollbackAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}

public sealed class SqlServerBookingTransactionFactory(Persistence.ServiceForgeDbContext dbContext)
    : IBookingTransactionFactory
{
    public async Task<IBookingTransaction> BeginAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            "SET TRANSACTION ISOLATION LEVEL READ COMMITTED",
            cancellationToken);
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new SqlServerBookingTransaction(transaction);
    }
}
