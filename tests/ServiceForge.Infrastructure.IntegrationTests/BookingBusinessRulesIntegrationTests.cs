using Microsoft.EntityFrameworkCore;
using ServiceForge.Application.Appointments.Allocation;
using ServiceForge.Application.Appointments.Rules;
using ServiceForge.Infrastructure.Persistence;
using ServiceForge.Infrastructure.Persistence.Queries;
using ServiceForge.Domain.Time;

namespace ServiceForge.Infrastructure.IntegrationTests;

public sealed class BookingBusinessRulesIntegrationTests
{
    [Fact]
    public async Task Vehicle_ownership_and_cross_dealership_conflicts_are_queryable()
    {
        await using var db = CreateContext();
        var vehicleQueries = new VehicleBookingQueries(db);
        var interval = TestInterval();

        Assert.True(await vehicleQueries.IsVehicleOwnedByAsync(1, 1, CancellationToken.None));
        Assert.False(await vehicleQueries.IsVehicleOwnedByAsync(1, 2, CancellationToken.None));

        await using var transaction = await db.Database.BeginTransactionAsync();
        await InsertAppointmentAsync(db, 910001, 1, 1, 2, 1, 3, 3, interval);

        var overlappingAtAnotherDealership = await vehicleQueries.HasOccupiedOverlapAsync(1, interval, CancellationToken.None);
        var adjacent = await vehicleQueries.HasOccupiedOverlapAsync(1, AdjacentInterval(), CancellationToken.None);

        Assert.True(overlappingAtAnotherDealership);
        Assert.False(adjacent);
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Different_vehicles_have_independent_conflict_checks()
    {
        await using var db = CreateContext();
        var vehicleQueries = new VehicleBookingQueries(db);

        Assert.False(await vehicleQueries.HasOccupiedOverlapAsync(1, TestInterval(), CancellationToken.None));
        Assert.False(await vehicleQueries.HasOccupiedOverlapAsync(2, TestInterval(), CancellationToken.None));
    }

    [Fact]
    public async Task Qualifications_compatibility_activity_and_occupancy_drive_resource_selection()
    {
        await using var db = CreateContext();
        var resourceQueries = new BookingResourceQueries(db);
        var interval = TestInterval();

        var initial = await resourceQueries.GetEligibilityAsync(1, 1, interval, CancellationToken.None);
        var initialAllocation = ResourceAllocator.SelectLowestAvailable(initial);
        Assert.True(initialAllocation.IsValid);
        Assert.Equal(1, initialAllocation.Value!.TechnicianId);
        Assert.Equal(1, initialAllocation.Value.ServiceBayId);

        await using var transaction = await db.Database.BeginTransactionAsync();
        await InsertAppointmentAsync(db, 910002, 1, 1, 1, 1, 1, 1, interval);

        var occupied = await resourceQueries.GetEligibilityAsync(1, 1, interval, CancellationToken.None);
        var fallback = ResourceAllocator.SelectLowestAvailable(occupied);
        Assert.True(fallback.IsValid);
        Assert.Equal(2, fallback.Value!.TechnicianId);
        Assert.Equal(2, fallback.Value.ServiceBayId);

        await db.Database.ExecuteSqlRawAsync("UPDATE [Technician] SET [IsActive] = 0 WHERE [Id] = 2;");
        await db.Database.ExecuteSqlRawAsync("UPDATE [ServiceBay] SET [IsActive] = 0 WHERE [Id] = 2;");
        var unavailable = await resourceQueries.GetEligibilityAsync(1, 1, interval, CancellationToken.None);
        Assert.Equal(BookingRuleErrorCodes.NoAvailableResources, ResourceEligibilityRules.GetErrorCode(unavailable));

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task Missing_qualification_and_compatibility_are_distinguished_from_occupancy()
    {
        await using var db = CreateContext();
        var resourceQueries = new BookingResourceQueries(db);

        var noQualifiedTechnician = await resourceQueries.GetEligibilityAsync(1, 3, TestInterval(), CancellationToken.None);
        Assert.False(noQualifiedTechnician.HasQualifiedTechnician);
        Assert.Equal(BookingRuleErrorCodes.NoQualifiedTechnician, ResourceEligibilityRules.GetErrorCode(noQualifiedTechnician));

        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO [TechnicianQualification] ([TechnicianId], [ServiceTypeId]) VALUES (3, 1);");
        var noCompatibleBay = await resourceQueries.GetEligibilityAsync(2, 1, TestInterval(), CancellationToken.None);
        Assert.False(noCompatibleBay.HasCompatibleBay);
        Assert.Equal(BookingRuleErrorCodes.NoCompatibleBay, ResourceEligibilityRules.GetErrorCode(noCompatibleBay));
        await transaction.RollbackAsync();
    }

    private static ServiceForgeDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("SERVICEFORGE_TEST_SQLSERVER_CONNECTION")
            ?? throw new InvalidOperationException("SERVICEFORGE_TEST_SQLSERVER_CONNECTION is required.");
        var options = new DbContextOptionsBuilder<ServiceForgeDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new ServiceForgeDbContext(options);
    }

    private static BookingInterval TestInterval() => BookingInterval.Create(
        new DateTime(2030, 1, 2, 9, 0, 0, DateTimeKind.Utc),
        new DateTime(2030, 1, 2, 10, 0, 0, DateTimeKind.Utc));

    private static BookingInterval AdjacentInterval() => BookingInterval.Create(
        new DateTime(2030, 1, 2, 10, 0, 0, DateTimeKind.Utc),
        new DateTime(2030, 1, 2, 11, 0, 0, DateTimeKind.Utc));

    private static Task<int> InsertAppointmentAsync(
        ServiceForgeDbContext db,
        long appointmentId,
        long customerId,
        long vehicleId,
        long dealershipId,
        long serviceTypeId,
        long technicianId,
        long serviceBayId,
        BookingInterval interval) =>
        db.Database.ExecuteSqlInterpolatedAsync($"""
            SET IDENTITY_INSERT [Appointment] ON;
            INSERT INTO [Appointment]
                ([Id], [CustomerId], [VehicleId], [DealershipId], [ServiceTypeId], [TechnicianId], [ServiceBayId],
                 [ScheduledStartUtc], [ScheduledEndUtc], [DurationMinutes], [Status], [AssignmentStatus],
                 [ActualCompletedAtUtc], [CreatedAtUtc])
            VALUES
                ({appointmentId}, {customerId}, {vehicleId}, {dealershipId}, {serviceTypeId}, {technicianId}, {serviceBayId},
                 {interval.StartUtc}, {interval.EndUtc}, 60, 'Confirmed', 'Assigned', NULL, SYSUTCDATETIME());
            SET IDENTITY_INSERT [Appointment] OFF;
            """);
}
