using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using ServiceForge.Application;
using ServiceForge.Application.Appointments.Contracts;
using ServiceForge.Application.Abstractions;
using ServiceForge.Domain.Entities;
using ServiceForge.Domain.Enums;
using ServiceForge.Infrastructure;
using ServiceForge.Infrastructure.Persistence;
using ServiceForge.Infrastructure.Persistence.Transactions;

namespace ServiceForge.Infrastructure.IntegrationTests;

[Collection("Shared SQL Server integration")]
public sealed class BookingConcurrencyIntegrationTests
{
    [Fact]
    public async Task Identical_same_key_requests_create_one_appointment_and_replay_the_same_response()
    {
        var key = Key();
        try
        {
            var results = await RunConcurrently(
                Command(1, 1, 1, 1, "2027-01-04T09:00:00Z", key),
                Command(1, 1, 1, 1, "2027-01-04T09:00:00Z", key));

            Assert.All(results, result => Assert.True(result.IsSuccess));
            Assert.Single(results.Select(result => result.Response!.Id).Distinct());
            Assert.Single(results.Select(result => result.Location).Distinct());
            Assert.Contains(results, result => result.IsReplay);
            Assert.Equal(1, await CountAppointmentsAsync(key));
        }
        finally
        {
            await CleanupAsync(key);
        }
    }

    [Fact]
    public async Task Different_keys_for_the_same_vehicle_allow_at_most_one_overlapping_booking()
    {
        var keys = new[] { Key(), Key() };
        try
        {
            var results = await RunConcurrently(
                Command(1, 1, 1, 1, "2027-01-05T09:00:00Z", keys[0]),
                Command(1, 1, 1, 1, "2027-01-05T09:00:00Z", keys[1]));

            Assert.Single(results, result => result.IsSuccess);
            Assert.Single(results, result => result.ErrorCode == "VEHICLE_TIME_CONFLICT");
            Assert.Equal(1, await CountAppointmentsAsync(keys));
        }
        finally
        {
            await CleanupAsync(keys);
        }
    }

    [Fact]
    public async Task Same_vehicle_overlap_is_serialized_across_different_dealerships()
    {
        var keys = new[] { Key(), Key() };
        try
        {
            var results = await RunConcurrently(
                Command(1, 1, 1, 1, "2027-01-12T09:00:00Z", keys[0]),
                Command(1, 1, 2, 2, "2027-01-12T09:30:00Z", keys[1]));

            Assert.Single(results, result => result.IsSuccess);
            Assert.Single(results, result => result.ErrorCode == "VEHICLE_TIME_CONFLICT");
            Assert.Equal(1, await CountAppointmentsAsync(keys));
        }
        finally
        {
            await CleanupAsync(keys);
        }
    }

    [Fact]
    public async Task Different_requests_using_the_same_key_return_one_success_and_one_conflict()
    {
        var key = Key();
        try
        {
            var results = await RunConcurrently(
                Command(1, 1, 1, 1, "2027-01-11T08:00:00Z", key),
                Command(1, 1, 1, 1, "2027-01-11T09:00:00Z", key));

            Assert.Single(results, result => result.IsSuccess);
            Assert.Single(results, result => result.ErrorCode == "IDEMPOTENCY_KEY_CONFLICT");
            Assert.Equal(1, await CountAppointmentsAsync(key));
        }
        finally
        {
            await CleanupAsync(key);
        }
    }

    [Fact]
    public async Task Different_vehicles_are_assigned_distinct_resources_when_the_dealership_has_capacity()
    {
        var keys = new[] { Key(), Key() };
        try
        {
            var results = await RunConcurrently(
                Command(1, 1, 1, 1, "2027-01-06T09:00:00Z", keys[0]),
                Command(2, 2, 1, 1, "2027-01-06T09:00:00Z", keys[1]));

            Assert.All(results, result => Assert.True(result.IsSuccess));
            Assert.Equal(2, results.Select(result => result.Response!.TechnicianId).Distinct().Count());
            Assert.Equal(2, results.Select(result => result.Response!.ServiceBayId).Distinct().Count());
        }
        finally
        {
            await CleanupAsync(keys);
        }
    }

    [Fact]
    public async Task Matching_replay_succeeds_while_vehicle_and_dealership_locks_are_held()
    {
        var key = Key();
        await using var blockerConnection = new SqlConnection(ConnectionString());
        try
        {
            var first = await SendAsync(Command(1, 1, 1, 1, "2027-01-07T09:00:00Z", key));
            Assert.True(first.IsSuccess);

            await blockerConnection.OpenAsync();
            await using var blockerTransaction = await blockerConnection.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            var lockManager = new SqlServerBookingLockManager();
            await lockManager.AcquireAsync(blockerTransaction, BookingLockKind.Vehicle, "1");
            await lockManager.AcquireAsync(blockerTransaction, BookingLockKind.Dealership, "1");

            var replay = await SendAsync(Command(1, 1, 1, 1, "2027-01-07T09:00:00Z", key));

            Assert.True(replay.IsSuccess);
            Assert.True(replay.IsReplay);
            Assert.Equal(first.Response, replay.Response);
            await blockerTransaction.RollbackAsync();
        }
        finally
        {
            await CleanupAsync(key);
        }
    }

    [Fact]
    public async Task Expired_key_reuse_creates_a_new_booking_and_retains_the_original()
    {
        var key = Key();
        long originalId = 0;
        try
        {
            await using (var db = CreateContext())
            await using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted))
            {
                var original = new Appointment(
                    0, 1, 1, 1, 1, 1, 1,
                    new DateTime(2026, 1, 5, 9, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc),
                    60, AppointmentStatus.Confirmed, AssignmentStatus.Assigned, null,
                    new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                db.Appointments.Add(original);
                await db.SaveChangesAsync();
                originalId = original.Id;
                db.IdempotencyRecords.Add(new IdempotencyRecord(
                    key, new byte[32], originalId, 201, "{}",
                    new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)));
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            var replacement = await SendAsync(Command(2, 2, 1, 1, "2027-01-08T09:00:00Z", key));

            Assert.True(replacement.IsSuccess);
            Assert.NotEqual(originalId, replacement.Response!.Id);
            await using var verify = CreateContext();
            Assert.True(await verify.Appointments.AnyAsync(item => item.Id == originalId));
            Assert.Equal(replacement.Response.Id,
                await verify.IdempotencyRecords.Where(item => item.Key == key).Select(item => item.AppointmentId).SingleAsync());
        }
        finally
        {
            await CleanupAsync(key);
            await DeleteAppointmentAsync(originalId);
        }
    }

    [Fact]
    public async Task Failed_booking_does_not_leave_an_idempotency_record_or_orphaned_appointment()
    {
        var key = Key();
        try
        {
            var result = await SendAsync(Command(1, 999999, 1, 1, "2027-01-09T09:00:00Z", key));

            Assert.False(result.IsSuccess);
            Assert.Equal("RESOURCE_NOT_FOUND", result.ErrorCode);
            Assert.Equal(0, await CountAppointmentsAsync(key));
        }
        finally
        {
            await CleanupAsync(key);
        }
    }

    [Fact]
    public async Task Held_idempotency_and_vehicle_locks_map_to_the_approved_transient_outcomes()
    {
        var idempotencyKey = Key();
        var vehicleKey = Key();
        await using var connection = new SqlConnection(ConnectionString());
        await connection.OpenAsync();
        var lockManager = new SqlServerBookingLockManager();

        await using (var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            await lockManager.AcquireAsync(transaction, BookingLockKind.IdempotencyKey, idempotencyKey);
            var result = await SendAsync(Command(1, 1, 1, 1, "2027-01-11T09:00:00Z", idempotencyKey));
            Assert.Equal("REQUEST_IN_PROGRESS", result.ErrorCode);
            Assert.Equal(409, result.StatusCode);
            await transaction.RollbackAsync();
        }

        await using (var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            await lockManager.AcquireAsync(transaction, BookingLockKind.Vehicle, "1");
            var result = await SendAsync(Command(1, 1, 1, 1, "2027-01-11T11:00:00Z", vehicleKey));
            Assert.Equal("BOOKING_TEMPORARILY_UNAVAILABLE", result.ErrorCode);
            Assert.Equal(503, result.StatusCode);
            await transaction.RollbackAsync();
        }
    }

    private static CreateAppointmentCommand Command(
        long customerId,
        long vehicleId,
        long dealershipId,
        long serviceTypeId,
        string startTime,
        string key) => new(
            new CreateAppointmentRequest(customerId, vehicleId, dealershipId, serviceTypeId, startTime), key);

    private static async Task<BookingOutcome[]> RunConcurrently(params CreateAppointmentCommand[] commands)
    {
        using var barrier = new Barrier(commands.Length);
        var tasks = commands.Select(command => Task.Run(async () =>
        {
            barrier.SignalAndWait(TimeSpan.FromSeconds(10));
            return await SendAsync(command);
        })).ToArray();
        return await Task.WhenAll(tasks);
    }

    private static async Task<BookingOutcome> SendAsync(CreateAppointmentCommand command)
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IMediator>().Send(command);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddServiceForgeApplication();
        services.AddServiceForgeInfrastructure();
        services.AddDbContext<ServiceForgeDbContext>(options => options.UseSqlServer(ConnectionString()));
        return services.BuildServiceProvider();
    }

    private static ServiceForgeDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ServiceForgeDbContext>().UseSqlServer(ConnectionString()).Options);

    private static async Task<int> CountAppointmentsAsync(params string[] keys)
    {
        await using var db = CreateContext();
        return await db.IdempotencyRecords.Where(item => keys.Contains(item.Key)).CountAsync();
    }

    private static async Task CleanupAsync(params string[] keys)
    {
        await using var db = CreateContext();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        var records = await db.IdempotencyRecords.Where(item => keys.Contains(item.Key)).ToListAsync();
        var appointmentIds = records.Select(item => item.AppointmentId).ToArray();
        db.IdempotencyRecords.RemoveRange(records);
        db.Appointments.RemoveRange(await db.Appointments.Where(item => appointmentIds.Contains(item.Id)).ToListAsync());
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private static async Task DeleteAppointmentAsync(long appointmentId)
    {
        if (appointmentId <= 0) return;
        await using var db = CreateContext();
        await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [Appointment] WHERE [Id] = {appointmentId};");
    }

    private static string ConnectionString() =>
        Environment.GetEnvironmentVariable("SERVICEFORGE_TEST_SQLSERVER_CONNECTION")
        ?? throw new InvalidOperationException("SERVICEFORGE_TEST_SQLSERVER_CONNECTION is required.");

    private static string Key() => $"batch4b-{Guid.NewGuid():N}";
}
