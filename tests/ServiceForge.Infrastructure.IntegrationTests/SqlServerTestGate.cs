namespace ServiceForge.Infrastructure.IntegrationTests;

public sealed class SqlServerTestGate : IAsyncLifetime
{
    private Semaphore? semaphore;

    public Task InitializeAsync()
    {
        semaphore = new Semaphore(1, 1, "ServiceForge.SharedSqlServerIntegrationTests");
        semaphore.WaitOne();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        semaphore?.Release();
        semaphore?.Dispose();
        return Task.CompletedTask;
    }
}

[CollectionDefinition("Shared SQL Server integration", DisableParallelization = true)]
public sealed class SharedSqlServerIntegrationCollection : ICollectionFixture<SqlServerTestGate>;
