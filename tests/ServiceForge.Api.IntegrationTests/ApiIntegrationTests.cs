using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.SqlClient;
using ServiceForge.Application.Abstractions;
using ServiceForge.Infrastructure.Persistence.Transactions;
using ServiceForge.Infrastructure.Persistence;

namespace ServiceForge.Api.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:ServiceForge", ConnectionString());
    }

    public async Task EnsureDatabaseAsync()
    {
        await using var db = new ServiceForgeDbContext(new DbContextOptionsBuilder<ServiceForgeDbContext>()
            .UseSqlServer(ConnectionString())
            .Options);
        await db.Database.MigrateAsync();
    }

    public async Task CleanupAsync(params string[] keys)
    {
        await using var db = new ServiceForgeDbContext(new DbContextOptionsBuilder<ServiceForgeDbContext>()
            .UseSqlServer(ConnectionString())
            .Options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var records = await db.IdempotencyRecords.Where(item => keys.Contains(item.Key)).ToListAsync();
        var ids = records.Select(item => item.AppointmentId).ToArray();
        db.IdempotencyRecords.RemoveRange(records);
        db.Appointments.RemoveRange(await db.Appointments.Where(item => ids.Contains(item.Id)).ToListAsync());
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public ServiceForgeDbContext CreateContext() => new(new DbContextOptionsBuilder<ServiceForgeDbContext>()
        .UseSqlServer(ConnectionString())
        .Options);

    private static string ConnectionString() =>
        Environment.GetEnvironmentVariable("SERVICEFORGE_TEST_SQLSERVER_CONNECTION")
        ?? throw new InvalidOperationException("SERVICEFORGE_TEST_SQLSERVER_CONNECTION is required.");
}

[Collection("Shared SQL Server integration")]
public sealed class ApiIntegrationTests(ApiFactory factory)
{
    [Fact]
    public async Task Post_creates_appointment_with_location_and_get_returns_it()
    {
        await factory.EnsureDatabaseAsync();
        var key = $"api-{Guid.NewGuid():N}";
        try
        {
            using var client = factory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/appointments")
            {
                Content = JsonContent.Create(new { customerId = 1, vehicleId = 1, dealershipId = 1, serviceTypeId = 1, startTime = "2027-02-01T09:00:00Z" })
            };
            request.Headers.Add("Idempotency-Key", key);
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal($"/api/appointments/{body.GetProperty("id").GetInt64()}", response.Headers.Location!.OriginalString);
            var get = await client.GetAsync(response.Headers.Location);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        }
        finally { await factory.CleanupAsync(key); }
    }

    [Fact]
    public async Task Matching_replay_preserves_body_and_location_and_conflict_returns_problem_details()
    {
        await factory.EnsureDatabaseAsync();
        var key = $"api-{Guid.NewGuid():N}";
        try
        {
            using var client = factory.CreateClient();
            var first = await SendBookingAsync(client, key, "2027-02-02T09:00:00Z");
            var replay = await SendBookingAsync(client, key, "2027-02-02T09:00:00Z");
            var conflict = await SendBookingAsync(client, key, "2027-02-02T10:00:00Z");

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
            Assert.Equal(first.Headers.Location, replay.Headers.Location);
            Assert.Equal(await first.Content.ReadAsStringAsync(), await replay.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.False(conflict.Headers.Contains("Retry-After"));
            var problem = await conflict.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("IDEMPOTENCY_KEY_CONFLICT", problem.GetProperty("code").GetString());
        }
        finally { await factory.CleanupAsync(key); }
    }

    [Fact]
    public async Task Equivalent_offset_replay_recovers_a_committed_booking_after_response_loss()
    {
        await factory.EnsureDatabaseAsync();
        var key = $"api-{Guid.NewGuid():N}";
        try
        {
            using var client = factory.CreateClient();
            var original = await SendBookingAsync(client, key, "2027-02-08T16:00:00+07:00");
            var originalBody = await original.Content.ReadFromJsonAsync<JsonElement>();
            var appointmentId = originalBody.GetProperty("id").GetInt64();
            var location = original.Headers.Location!.OriginalString;
            original.Dispose();

            await using (var db = factory.CreateContext())
            {
                Assert.True(await db.Appointments.AnyAsync(item => item.Id == appointmentId));
                Assert.Equal(appointmentId, await db.IdempotencyRecords
                    .Where(item => item.Key == key)
                    .Select(item => item.AppointmentId)
                    .SingleAsync());
            }

            var retry = await SendBookingAsync(client, key, "2027-02-08T09:00:00Z");
            var retryBody = await retry.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
            Assert.Equal(location, retry.Headers.Location!.OriginalString);
            Assert.Equal(originalBody.GetRawText(), retryBody);
            await using var verify = factory.CreateContext();
            Assert.Equal(1, await verify.IdempotencyRecords.CountAsync(item => item.Key == key));
            Assert.Equal(1, await verify.Appointments.CountAsync(item => item.Id == appointmentId));
        }
        finally { await factory.CleanupAsync(key); }
    }

    [Fact]
    public async Task Validation_and_missing_key_return_problem_details_and_lists_are_paginated()
    {
        await factory.EnsureDatabaseAsync();
        using var client = factory.CreateClient();
        var missingKey = await client.PostAsJsonAsync("/api/appointments", new { customerId = 1, vehicleId = 1, dealershipId = 1, serviceTypeId = 1, startTime = "2027-02-03T09:00:00Z" });
        var invalidTime = await SendBookingAsync(client, $"api-{Guid.NewGuid():N}", "2027-02-03T09:00:00");
        var vehicles = await client.GetFromJsonAsync<JsonElement>("/api/customers/1/vehicles?page=1&pageSize=1");
        var dealerships = await client.GetFromJsonAsync<JsonElement>("/api/dealerships");
        var serviceTypes = await client.GetFromJsonAsync<JsonElement>("/api/service-types?pageSize=100");
        var notFound = await client.GetAsync("/api/appointments/999999999");

        Assert.Equal(HttpStatusCode.BadRequest, missingKey.StatusCode);
        Assert.Equal("INVALID_REQUEST", (await missingKey.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, invalidTime.StatusCode);
        Assert.Equal("INVALID_BOOKING_TIME", (await invalidTime.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        var invalidPage = await client.GetAsync("/api/dealerships?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, invalidPage.StatusCode);
        Assert.Equal("INVALID_REQUEST", (await invalidPage.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        Assert.Equal(1, vehicles.GetProperty("pageSize").GetInt32());
        Assert.True(dealerships.GetProperty("items").GetArrayLength() > 0);
        Assert.True(serviceTypes.GetProperty("items").GetArrayLength() > 0);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Development_openapi_describes_all_five_approved_endpoints()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        var root = document.RootElement;
        Assert.True(root.TryGetProperty("openapi", out var version));
        Assert.Equal(JsonValueKind.String, version.ValueKind);
        Assert.True(root.TryGetProperty("paths", out var paths));
        Assert.Equal(JsonValueKind.Object, paths.ValueKind);

        var expected = new[]
        {
            (Path: "/api/appointments", Method: "post"),
            (Path: "/api/appointments/{id}", Method: "get"),
            (Path: "/api/customers/{id}/vehicles", Method: "get"),
            (Path: "/api/dealerships", Method: "get"),
            (Path: "/api/service-types", Method: "get")
        };

        foreach (var endpoint in expected)
        {
            Assert.True(paths.TryGetProperty(endpoint.Path, out var pathItem), endpoint.Path);
            Assert.True(pathItem.TryGetProperty(endpoint.Method, out _),
                $"{endpoint.Method.ToUpperInvariant()} {endpoint.Path}");
        }
    }

    [Fact]
    public async Task Ownership_failure_and_vehicle_conflict_use_approved_problem_details()
    {
        await factory.EnsureDatabaseAsync();
        var conflictKey = $"api-{Guid.NewGuid():N}";
        var secondKey = $"api-{Guid.NewGuid():N}";
        try
        {
            using var client = factory.CreateClient();
            var ownership = await SendBookingAsync(client, $"api-{Guid.NewGuid():N}", "2027-02-04T09:00:00Z", customerId: 2);
            var first = await SendBookingAsync(client, conflictKey, "2027-02-05T09:00:00Z");
            var conflict = await SendBookingAsync(client, secondKey, "2027-02-05T09:00:00Z");

            Assert.Equal(HttpStatusCode.Forbidden, ownership.StatusCode);
            Assert.Equal("VEHICLE_NOT_OWNED", (await ownership.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
            Assert.Equal("VEHICLE_TIME_CONFLICT", (await conflict.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        }
        finally { await factory.CleanupAsync(conflictKey, secondKey); }
    }

    [Fact]
    public async Task Every_approved_http_problem_mapping_returns_the_approved_code_and_status()
    {
        await factory.EnsureDatabaseAsync();
        using var client = factory.CreateClient();
        var invalidRequest = await client.PostAsJsonAsync("/api/appointments", new { customerId = 0, vehicleId = 1, dealershipId = 1, serviceTypeId = 1, startTime = "2027-02-09T09:00:00Z" });
        var invalidTime = await SendBookingAsync(client, $"api-{Guid.NewGuid():N}", "2027-02-09T09:00:01Z");
        var outsideHours = await SendBookingAsync(client, $"api-{Guid.NewGuid():N}", "2027-02-07T09:00:00Z");
        var horizon = await SendBookingAsync(client, $"api-{Guid.NewGuid():N}", "2028-01-03T09:00:00Z");
        var notFound = await client.GetAsync("/api/appointments/999999998");

        await AssertProblemAsync(invalidRequest, 400, "INVALID_REQUEST");
        await AssertProblemAsync(invalidTime, 400, "INVALID_BOOKING_TIME");
        await AssertProblemAsync(outsideHours, 400, "OUTSIDE_OPERATING_HOURS");
        await AssertProblemAsync(horizon, 400, "BOOKING_HORIZON_EXCEEDED");
        await AssertProblemAsync(notFound, 404, "RESOURCE_NOT_FOUND");

        var ownership = await SendBookingAsync(client, $"api-{Guid.NewGuid():N}", "2027-02-10T09:00:00Z", 2);
        await AssertProblemAsync(ownership, 403, "VEHICLE_NOT_OWNED");

        var conflictKey1 = $"api-{Guid.NewGuid():N}";
        var conflictKey2 = $"api-{Guid.NewGuid():N}";
        try
        {
            var created = await SendBookingAsync(client, conflictKey1, "2027-02-11T09:00:00Z");
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            await AssertProblemAsync(await SendBookingAsync(client, conflictKey2, "2027-02-11T09:00:00Z"), 409, "VEHICLE_TIME_CONFLICT");
        }
        finally { await factory.CleanupAsync(conflictKey1, conflictKey2); }

        var conflictKey = $"api-{Guid.NewGuid():N}";
        try
        {
            await AssertProblemAsync(await SendBookingAsync(client, conflictKey, "2027-02-12T08:00:00Z", 1, 1, 1, 3), 409, "NO_QUALIFIED_TECHNICIAN");
        }
        finally { await factory.CleanupAsync(conflictKey); }

        await using (var db = factory.CreateContext())
        {
            await db.Database.ExecuteSqlRawAsync("INSERT INTO [TechnicianQualification] ([TechnicianId], [ServiceTypeId]) VALUES (3, 1);");
        }
        var compatibilityKey = $"api-{Guid.NewGuid():N}";
        try
        {
            await AssertProblemAsync(await SendBookingAsync(client, compatibilityKey, "2027-02-15T09:00:00Z", 1, 1, 2, 1), 409, "NO_COMPATIBLE_BAY");
        }
        finally
        {
            await using var db = factory.CreateContext();
            await db.Database.ExecuteSqlRawAsync("DELETE FROM [TechnicianQualification] WHERE [TechnicianId] = 3 AND [ServiceTypeId] = 1;");
            await factory.CleanupAsync(compatibilityKey);
        }

        var occupiedKey = $"api-{Guid.NewGuid():N}";
        var unavailableKey = $"api-{Guid.NewGuid():N}";
        try
        {
            var occupied = await SendBookingAsync(client, occupiedKey, "2027-02-16T09:00:00Z", 1, 1, 2, 2);
            Assert.Equal(HttpStatusCode.Created, occupied.StatusCode);
            await AssertProblemAsync(await SendBookingAsync(client, unavailableKey, "2027-02-16T09:00:00Z", 2, 2, 2, 2), 409, "NO_AVAILABLE_RESOURCES");
        }
        finally
        {
            await factory.CleanupAsync(occupiedKey, unavailableKey);
        }

        var inProgressKey = $"api-{Guid.NewGuid():N}";
        await using var connection = new SqlConnection(Environment.GetEnvironmentVariable("SERVICEFORGE_TEST_SQLSERVER_CONNECTION"));
        await connection.OpenAsync();
        await using (var transaction = await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            await new SqlServerBookingLockManager().AcquireAsync(transaction, BookingLockKind.IdempotencyKey, inProgressKey);
            var inProgress = await SendBookingAsync(client, inProgressKey, "2027-02-17T09:00:00Z");
            await AssertProblemAsync(inProgress, 409, "REQUEST_IN_PROGRESS", true);
            await transaction.RollbackAsync();
        }

        var unavailableKey2 = $"api-{Guid.NewGuid():N}";
        await using var connection2 = new SqlConnection(Environment.GetEnvironmentVariable("SERVICEFORGE_TEST_SQLSERVER_CONNECTION"));
        await connection2.OpenAsync();
        await using (var transaction = await connection2.BeginTransactionAsync(IsolationLevel.ReadCommitted))
        {
            await new SqlServerBookingLockManager().AcquireAsync(transaction, BookingLockKind.Vehicle, "1");
            var unavailable = await SendBookingAsync(client, unavailableKey2, "2027-02-18T09:00:00Z");
            await AssertProblemAsync(unavailable, 503, "BOOKING_TEMPORARILY_UNAVAILABLE", true);
            await transaction.RollbackAsync();
        }
    }

    private static async Task<HttpResponseMessage> SendBookingAsync(HttpClient client, string key, string startTime, long customerId = 1, long vehicleId = 1, long dealershipId = 1, long serviceTypeId = 1)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/appointments")
        {
            Content = JsonContent.Create(new { customerId, vehicleId, dealershipId, serviceTypeId, startTime })
        };
        request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, int statusCode, string code, bool retryAfter = false)
    {
        Assert.Equal((HttpStatusCode)statusCode, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(code, problem.GetProperty("code").GetString());
        if (retryAfter)
        {
            Assert.Equal(TimeSpan.FromSeconds(2), response.Headers.RetryAfter!.Delta);
        }
    }
}
