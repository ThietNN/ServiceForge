using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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

    private static string ConnectionString() =>
        Environment.GetEnvironmentVariable("SERVICEFORGE_TEST_SQLSERVER_CONNECTION")
        ?? throw new InvalidOperationException("SERVICEFORGE_TEST_SQLSERVER_CONNECTION is required.");
}

public sealed class ApiIntegrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
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
            var problem = await conflict.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("IDEMPOTENCY_KEY_CONFLICT", problem.GetProperty("code").GetString());
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

    private static async Task<HttpResponseMessage> SendBookingAsync(HttpClient client, string key, string startTime, long customerId = 1)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/appointments")
        {
            Content = JsonContent.Create(new { customerId, vehicleId = 1, dealershipId = 1, serviceTypeId = 1, startTime })
        };
        request.Headers.Add("Idempotency-Key", key);
        return await client.SendAsync(request);
    }
}
