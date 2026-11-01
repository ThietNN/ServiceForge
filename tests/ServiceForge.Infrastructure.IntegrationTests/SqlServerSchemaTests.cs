using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ServiceForge.Infrastructure.Persistence;

namespace ServiceForge.Infrastructure.IntegrationTests;

public sealed class SqlServerSchemaTests : IAsyncLifetime
{
    private string ConnectionString => Environment.GetEnvironmentVariable("SERVICEFORGE_TEST_SQLSERVER_CONNECTION")
        ?? throw new InvalidOperationException(
            "SERVICEFORGE_TEST_SQLSERVER_CONNECTION must target the dedicated ServiceForge integration-test database.");

    public async Task InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Migration_creates_ten_tables_seed_data_and_read_committed_snapshot_is_off()
    {
        await using var db = CreateContext();
        var tableCount = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE is_ms_shipped = 0 AND name <> '__EFMigrationsHistory'").SingleAsync();
        var databaseOptions = await db.Database.SqlQueryRaw<int>(
            "SELECT CAST(is_read_committed_snapshot_on AS int) AS [Value] FROM sys.databases WHERE name = DB_NAME()").SingleAsync();
        var dealershipCount = await db.Dealerships.CountAsync();
        var serviceTypeCount = await db.ServiceTypes.CountAsync();

        Assert.Equal(10, tableCount);
        Assert.Equal(0, databaseOptions);
        Assert.Equal(3, dealershipCount);
        Assert.Equal(3, serviceTypeCount);
    }

    [Fact]
    public async Task Appointment_idempotency_index_is_unique_and_foreign_keys_are_non_cascading()
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sys.foreign_keys
            WHERE delete_referential_action_desc <> 'NO_ACTION'
              AND parent_object_id IN (OBJECT_ID('Appointment'), OBJECT_ID('IdempotencyRecord'));
            SELECT COUNT(*)
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('IdempotencyRecord')
              AND name = 'IX_IdempotencyRecord_AppointmentId'
              AND is_unique = 1;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(0, reader.GetInt32(0));
        Assert.True(await reader.NextResultAsync());
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1, reader.GetInt32(0));
    }

    private ServiceForgeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ServiceForgeDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        return new ServiceForgeDbContext(options);
    }
}
