using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ServiceForge.Application.Abstractions;
using ServiceForge.Application.Appointments.Idempotency;
using ServiceForge.Domain.Entities;
using ServiceForge.Domain.Enums;
using ServiceForge.Infrastructure.Persistence;
using ServiceForge.Infrastructure.Persistence.Idempotency;
using ServiceForge.Infrastructure.Persistence.Transactions;
using ServiceForge.Infrastructure.Time;

namespace ServiceForge.Infrastructure.IntegrationTests;

public sealed class BookingInfrastructureIntegrationTests
{
    [Fact]
    public async Task Sql_server_clock_returns_authoritative_utc_time()
    {
        await using var connection = new SqlConnection(ConnectionString());
        await connection.OpenAsync();
        var clock = new SqlServerUtcClock();
        var before = DateTime.UtcNow;

        var value = await clock.GetUtcNowAsync(connection);

        var after = DateTime.UtcNow;
        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.InRange(value, before.AddSeconds(-2), after.AddSeconds(2));
    }

    [Fact]
    public async Task Transaction_owned_application_lock_times_out_then_is_released()
    {
        var value = Guid.NewGuid().ToString("N");
        await using var firstConnection = new SqlConnection(ConnectionString());
        await using var secondConnection = new SqlConnection(ConnectionString());
        await firstConnection.OpenAsync();
        await secondConnection.OpenAsync();
        await using var firstTransaction = await firstConnection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await using var secondTransaction = await secondConnection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var manager = new SqlServerBookingLockManager();

        await manager.AcquireAsync(firstTransaction, BookingLockKind.IdempotencyKey, value);
        var exception = await Assert.ThrowsAsync<BookingLockException>(() =>
            manager.AcquireAsync(secondTransaction, BookingLockKind.IdempotencyKey, value));

        Assert.Equal(-1, exception.ReturnCode);
        await secondTransaction.RollbackAsync();
        await firstTransaction.CommitAsync();

        await using var releasedTransaction = await firstConnection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        await manager.AcquireAsync(releasedTransaction, BookingLockKind.IdempotencyKey, value);
        await releasedTransaction.RollbackAsync();
    }

    [Fact]
    public async Task Atomic_persistence_replays_conflicts_replaces_expired_records_and_rolls_back()
    {
        await using var db = CreateContext();
        var store = new SqlServerIdempotencyStore(db);
        var persistence = new SqlServerAtomicBookingPersistence(db);
        var key = $"batch4-{Guid.NewGuid():N}";
        var hash = new byte[32];
        hash[0] = 7;
        var created = new DateTime(2031, 1, 2, 8, 0, 0, DateTimeKind.Utc);
        var expires = created.AddHours(24);
        long originalAppointmentId;
        long replacementAppointmentId;

        await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            var appointment = CreateAppointment();
            await persistence.PersistAsync(appointment, key, hash, 201, id => "{\"id\":1}", created, expires,
                CancellationToken.None);
            originalAppointmentId = appointment.Id;
            await transaction.CommitAsync();
        }

        var matching = await store.LookupAsync(key, hash, created.AddMinutes(1), CancellationToken.None);
        Assert.Equal(IdempotencyLookupStatus.MatchingUnexpired, matching.Status);
        Assert.Equal($"/api/appointments/{originalAppointmentId}", matching.Location);

        var conflictingHash = new byte[32];
        conflictingHash[0] = 8;
        var conflict = await store.LookupAsync(key, conflictingHash, created.AddMinutes(1), CancellationToken.None);
        Assert.Equal(IdempotencyLookupStatus.ConflictingUnexpired, conflict.Status);

        await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            var replacement = CreateAppointment();
            var replacementCreated = expires.AddMinutes(1);
            await persistence.PersistAsync(replacement, key, conflictingHash, 201, id => "{\"id\":2}", replacementCreated, replacementCreated.AddHours(24),
                CancellationToken.None);
            replacementAppointmentId = replacement.Id;
            await transaction.CommitAsync();
        }

        var expiredReplacement = await store.LookupAsync(key, conflictingHash, expires.AddMinutes(2), CancellationToken.None);
        Assert.Equal(IdempotencyLookupStatus.MatchingUnexpired, expiredReplacement.Status);
        Assert.Equal(replacementAppointmentId, expiredReplacement.Record!.AppointmentId);
        Assert.True(await db.Appointments.AnyAsync(item => item.Id == originalAppointmentId));

        await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            var records = await db.IdempotencyRecords.Where(item => item.Key == key).ToListAsync();
            db.IdempotencyRecords.RemoveRange(records);
            db.Appointments.RemoveRange(await db.Appointments
                .Where(item => item.Id == originalAppointmentId || item.Id == replacementAppointmentId)
                .ToListAsync());
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            var appointment = CreateAppointment();
            await persistence.PersistAsync(appointment, $"rollback-{Guid.NewGuid():N}", new byte[32], 201, _ => "{}", created, expires,
                CancellationToken.None);
            var rolledBackId = appointment.Id;
            await transaction.RollbackAsync();
            Assert.False(await db.Appointments.AnyAsync(item => item.Id == rolledBackId));
        }
    }

    private static string ConnectionString() =>
        Environment.GetEnvironmentVariable("SERVICEFORGE_TEST_SQLSERVER_CONNECTION")
        ?? throw new InvalidOperationException("SERVICEFORGE_TEST_SQLSERVER_CONNECTION is required.");

    private static ServiceForgeDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ServiceForgeDbContext>()
            .UseSqlServer(ConnectionString())
            .Options);

    private static Appointment CreateAppointment() => new(
        0, 1, 1, 1, 1, 1, 1,
        new DateTime(2031, 1, 2, 9, 0, 0, DateTimeKind.Utc),
        new DateTime(2031, 1, 2, 10, 0, 0, DateTimeKind.Utc),
        60, AppointmentStatus.Confirmed, AssignmentStatus.Assigned, null,
        new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
}
