# ServiceForge Reviewer Guide

This guide is a Windows-oriented walkthrough for the verified local SQL Server path. It uses the
repository's actual project names, migration, seed IDs, routes, and environment variables.

## 1. Prerequisites

Verified in the repository environment:

- Windows PowerShell
- .NET SDK 10.0.403
- `dotnet-ef` 10.0.14
- SQL Server with permission to create/use a dedicated database

The complete workflow has not been tested on macOS/Linux. Docker is not required or configured.

Clone the repository and enter it:

```powershell
git clone <repository-url> ServiceForge
Set-Location .\ServiceForge
```

The clone command is a standard checkout command and was not executed in this repository session.

## 2. Build the solution

```powershell
dotnet restore ServiceForge.slnx
dotnet build ServiceForge.slnx --no-restore
```

These commands were executed successfully during the final verification batches. The final
standalone build completed with zero warnings and errors.

## 3. Prepare SQL Server safely

Use a disposable database dedicated to this repository. Do not use a production database. In SSMS
or another authorized SQL Server client, create the database if needed:

```sql
IF DB_ID(N'ServiceForge') IS NULL
    CREATE DATABASE [ServiceForge];
```

This SQL was source-reviewed but was not executed during the documentation batch.

Set the application connection string without committing it:

```powershell
$env:SERVICEFORGE_CONNECTION_STRING = "Server=<approved-server>;Database=ServiceForge;Integrated Security=True;TrustServerCertificate=True;"
```

For SQL authentication, keep the user and password in the local environment or secret manager; do
not place them in source control.

### MediatR licensing notice

The application uses MediatR 14.2.0. Under the publisher's published terms, internal development
and testing are distinct from production use; no license key is required for the development and
testing workflow described here. Production deployment requires a separate licensing decision under
the publisher's current terms. This repository does not suppress the MediatR warning or embed a
license key. Review the [publisher's license agreement](https://luckypennysoftware.com/license)
before production use.

## 4. Apply the migration and seed data

The API does not automatically migrate or seed at startup. Apply the reviewed Code First migration:

```powershell
dotnet ef database update `
  --project src/ServiceForge.Infrastructure `
  --startup-project src/ServiceForge.Api `
  --context ServiceForgeDbContext
```

Migration `20261101173028_InitialCreate` creates the ten approved tables and applies deterministic
`HasData` reference data. The command was verified against the design-time factory and migration
paths by source inspection; the final test suites independently applied the migration successfully
to the dedicated integration database.

Seed IDs used by the examples include:

- Customer 1 owns Vehicle 1 (`SF-1001`); Customer 2 owns Vehicle 2 (`SF-2001`).
- Dealership 1 is Bangkok; ServiceType 1 is a 60-minute Oil Service.
- Dealership 1 has active technicians 1 and 2 and bays 1 and 2 qualified/compatible for ServiceType 1.

## 5. Start the API and find OpenAPI

```powershell
dotnet run --project src/ServiceForge.Api
```

In another PowerShell window, request the Development OpenAPI document:

```powershell
Invoke-WebRequest http://localhost:5009/openapi/v1.json | Select-Object -ExpandProperty Content
```

The port comes from `src/ServiceForge.Api/Properties/launchSettings.json`. OpenAPI is mapped only
in the Development environment. The OpenAPI integration test verifies all five approved operations.

## 6. Create and read an appointment

The following PowerShell block computes the next weekday at 10:00 in seeded Dealership 1's
`Asia/Bangkok` timezone, avoiding a hardcoded date that can expire:

```powershell
# Windows PowerShell uses the Windows ID for the seeded Asia/Bangkok (UTC+07:00) timezone.
$zone = [TimeZoneInfo]::FindSystemTimeZoneById("SE Asia Standard Time")
$localDate = [TimeZoneInfo]::ConvertTimeFromUtc([DateTime]::UtcNow, $zone).Date.AddDays(1)
while ($localDate.DayOfWeek -in @([DayOfWeek]::Saturday, [DayOfWeek]::Sunday)) {
    $localDate = $localDate.AddDays(1)
}
$localStart = [DateTime]::SpecifyKind($localDate.AddHours(10), [DateTimeKind]::Unspecified)
$utcStart = [TimeZoneInfo]::ConvertTimeToUtc($localStart, $zone)
$bookingStart = $utcStart.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'")
$key = "review-$(Get-Date -Format yyyyMMddHHmmss)-$([Guid]::NewGuid().ToString('N'))"
$body = @{
    customerId = 1
    vehicleId = 1
    dealershipId = 1
    serviceTypeId = 1
    startTime = $bookingStart
} | ConvertTo-Json
$headers = @{ "Idempotency-Key" = $key }
$created = Invoke-RestMethod http://localhost:5009/api/appointments -Method Post -Headers $headers -ContentType 'application/json' -Body $body -ResponseHeadersVariable responseHeaders
$created | ConvertTo-Json
$location = $responseHeaders.Location
Invoke-RestMethod "http://localhost:5009$location" | ConvertTo-Json
```

The endpoint returns `201 Created`, a `Location` header, a Confirmed/Assigned appointment, assigned
technician and bay IDs, and UTC timestamps. The dynamic timestamp generation was source-reviewed;
this demonstration was not executed during the documentation batch.

## 7. Demonstrate idempotency and a vehicle conflict

Replay the same request with the same `$key`, `$headers`, and `$body`:

```powershell
$replay = Invoke-WebRequest http://localhost:5009/api/appointments -Method Post -Headers $headers -ContentType 'application/json' -Body $body
$replay.StatusCode
$replay.Headers.Location
$replay.Content
```

The replay returns the stored successful response and original `Location` without creating another
appointment.

To demonstrate a genuine vehicle conflict, use a different key with the same customer, vehicle,
dealership, service type, and `$bookingStart`:

```powershell
$conflictHeaders = @{ "Idempotency-Key" = "review-conflict-$([Guid]::NewGuid().ToString('N'))" }
$conflict = Invoke-WebRequest http://localhost:5009/api/appointments -Method Post -Headers $conflictHeaders -ContentType 'application/json' -Body $body
$conflict.StatusCode
$conflict.Content
```

The expected result is HTTP 409 with Problem Details code `VEHICLE_TIME_CONFLICT`. This competes for
the same vehicle and interval; it does not rely on all dealership resources being occupied. Use a
dedicated disposable database and clean up demonstration records through an authorized database
maintenance process after review.

## 8. Run automated tests

Unit tests:

```powershell
dotnet test tests/ServiceForge.UnitTests/ServiceForge.UnitTests.csproj --no-restore --no-build
```

HTTP integration tests require the dedicated test database and environment variable:

```powershell
$env:SERVICEFORGE_TEST_SQLSERVER_CONNECTION = "Server=<approved-server>;Database=ServiceForge_IntegrationTests;Integrated Security=True;TrustServerCertificate=True;"
dotnet test tests/ServiceForge.Api.IntegrationTests/ServiceForge.Api.IntegrationTests.csproj --no-restore --no-build
```

SQL Server integration tests:

```powershell
dotnet test tests/ServiceForge.Infrastructure.IntegrationTests/ServiceForge.Infrastructure.IntegrationTests.csproj --no-restore --no-build
```

Concurrency suite, including the delayed-lock regression:

```powershell
dotnet test tests/ServiceForge.Infrastructure.IntegrationTests/ServiceForge.Infrastructure.IntegrationTests.csproj --no-restore --no-build --filter FullyQualifiedName~BookingConcurrencyIntegrationTests
```

The final R1 verification executed the unit, HTTP, SQL, and concurrency commands successfully. Do
not report a test as passing unless it was run successfully in the current environment.

## 9. Troubleshooting and limitations

- If EF commands report a missing connection, set `SERVICEFORGE_CONNECTION_STRING` in the same
  PowerShell session.
- If tests report a missing connection, set `SERVICEFORGE_TEST_SQLSERVER_CONNECTION` to the
  dedicated disposable test database.
- The application does not auto-migrate or seed at startup.
- The verified complete workflow is Windows-only. macOS/Linux setup and full execution remain
  unverified.
- Direct database writers can bypass the booking lock protocol.
- SQL deadlock classification is implemented, but no artificial SQL deadlock has been induced.
- Authentication, frontend work, Docker, and P1 workflows are outside the approved P0 scope.

## Related documentation

- [System design](./SYSTEM-DESIGN.md)
- [README](../README.md)
- [API contract](../specs/001-service-booking/contracts/api.md)
- [Acceptance traceability](./acceptance-traceability.md)
