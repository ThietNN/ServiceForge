# ServiceForge

ServiceForge is a backend-only ASP.NET Core Web API for P0 automotive service booking across
multiple dealerships. The implementation uses .NET 10, Clean Architecture, CQRS/MediatR, EF Core,
and SQL Server. Approved specifications in `specs/001-service-booking/` and the ratified
constitution are authoritative.

## Scope and architecture

The solution contains four production projects:

- `ServiceForge.Api` — HTTP endpoints, Problem Details, OpenAPI, and dependency composition.
- `ServiceForge.Application` — CQRS handlers, validation, orchestration, and application contracts.
- `ServiceForge.Domain` — framework-independent entities, rules, and value concepts.
- `ServiceForge.Infrastructure` — EF Core SQL Server persistence, transactions, locks, and queries.

P0 exposes five approved endpoints: create and retrieve appointments, and read paginated customer
vehicles, dealerships, and service types. Authentication, Angular, Playwright, Docker configuration,
microservices, message queues, and P1 lifecycle workflows are out of scope.

Reviewer documentation:

- [System design](docs/SYSTEM-DESIGN.md) — architecture, data flow, concurrency, idempotency,
  observability boundaries, and AI-assisted engineering process.
- [Reviewer guide](docs/REVIEWER-GUIDE.md) — Windows setup, migration, seeded demo, API walkthrough,
  idempotency/conflict checks, and test commands.

## Reviewer quick start

For a clean Windows walkthrough, use the [Reviewer Guide](docs/REVIEWER-GUIDE.md). In brief: set an
environment-only SQL Server connection, apply `20261101173028_InitialCreate` with `dotnet ef database
update`, start `ServiceForge.Api`, and use the guide's dynamic PowerShell timestamp block to create
an appointment against the seeded reference data.

Booking uses one SQL Server transaction at `READ COMMITTED` with `READ_COMMITTED_SNAPSHOT OFF`.
Transaction-owned exclusive application locks use the global order **Idempotency Key → Vehicle →
Dealership**, with a five-second timeout per lock. Matching idempotency requests replay the stored
successful response; mismatches return `IDEMPOTENCY_KEY_CONFLICT`. Appointment and successful
idempotency persistence are atomic. Direct database writers that bypass this protocol and induced
deadlock coverage remain known limitations.

## Prerequisites

- .NET SDK 10.0 or a later compatible 10.0 servicing release. The verified development SDK is
  10.0.403.
- SQL Server reachable from the development machine. Tests require a dedicated integration-test
  database; Docker/Testcontainers setup is not part of this repository.
- The `dotnet-ef` tool compatible with EF Core 10. The verified tool version is 10.0.14.
- PowerShell for the documented Windows commands. Bash/Zsh commands are provided for environments
  where the .NET SDK and SQL Server connection are otherwise available.

Windows integrated authentication can use a connection string such as:

```text
Server=<server>;Database=ServiceForge;Integrated Security=True;TrustServerCertificate=True;
```

SQL authentication can use placeholders such as:

```text
Server=<server>;Database=ServiceForge;User Id=<user>;Password=<password>;TrustServerCertificate=True;
```

Keep credentials in environment variables or a local secret manager. Do not commit connection
strings, passwords, `.env` files, or machine-specific configuration.

## Configuration and database initialization

The API reads `ConnectionStrings:ServiceForge` from normal ASP.NET Core configuration, then falls
back to the `SERVICEFORGE_CONNECTION_STRING` environment variable. The EF design-time factory uses
`SERVICEFORGE_CONNECTION_STRING` for migration commands. The application does **not** automatically
apply migrations or seed the database during startup.

Set a connection string and apply the reviewed Code First migration explicitly:

PowerShell:

```powershell
$env:SERVICEFORGE_CONNECTION_STRING = "Server=<server>;Database=ServiceForge;Integrated Security=True;TrustServerCertificate=True;"
dotnet ef database update --project src/ServiceForge.Infrastructure --startup-project src/ServiceForge.Api --context ServiceForgeDbContext
dotnet run --project src/ServiceForge.Api
```

Bash/Zsh:

```bash
export SERVICEFORGE_CONNECTION_STRING='Server=<server>;Database=ServiceForge;Integrated Security=True;TrustServerCertificate=True;'
dotnet ef database update --project src/ServiceForge.Infrastructure --startup-project src/ServiceForge.Api --context ServiceForgeDbContext
dotnet run --project src/ServiceForge.Api
```

The initial migration is `20261101173028_InitialCreate`. Reference data is deterministic EF Core
`HasData` included in that migration; there is no separate runtime seed command. Review any future
migration before applying it. In Development, OpenAPI is available at `/openapi/v1.json`.

## Spec Kit context

For read-only Spec Kit diagnostics in a shell without persisted feature context, use the supported
feature-directory override and disable persistence:

PowerShell:

```powershell
$env:SPECIFY_FEATURE_DIRECTORY = "specs/001-service-booking"
$env:SPECIFY_FEATURE_NO_PERSIST = "1"
```

`SPECIFY_FEATURE_NO_PERSIST=1` is for diagnostics and does not create `.specify/feature.json`.
The repository uses the PowerShell Spec Kit scripts under `.specify/scripts/powershell`. Invocation
through `pwsh` on macOS/Linux is not verified here; no cross-platform Spec Kit claim is made.

## API example

Create a booking with an explicit-offset ISO 8601 start and a required global idempotency key:

```http
POST /api/appointments HTTP/1.1
Content-Type: application/json
Idempotency-Key: example-booking-001

{
  "customerId": 1,
  "vehicleId": 1,
  "dealershipId": 1,
  "serviceTypeId": 1,
  "startTime": "2027-01-15T10:00:00+07:00"
}
```

A successful request returns `201 Created`, a `Location` header, the fully assigned Confirmed
appointment, and UTC timestamps. Errors use RFC 9457 Problem Details with a machine-readable
`code` extension. Approved transient outcomes include `Retry-After: 2`.

## Build and test

Unit tests do not require SQL Server. SQL-backed integration and HTTP tests require a dedicated
database supplied through `SERVICEFORGE_TEST_SQLSERVER_CONNECTION`; the test projects apply the
reviewed schema and clean up generated records. A named cross-host gate serializes the two SQL-backed
test hosts against that shared test database.

PowerShell:

```powershell
dotnet restore ServiceForge.slnx
dotnet build ServiceForge.slnx --no-restore
$env:SERVICEFORGE_TEST_SQLSERVER_CONNECTION = "<dedicated test database connection>"
dotnet test ServiceForge.slnx --no-restore --no-build
dotnet list ServiceForge.slnx package --include-transitive --vulnerable
git diff --check
```

Bash/Zsh:

```bash
dotnet restore ServiceForge.slnx
dotnet build ServiceForge.slnx --no-restore
export SERVICEFORGE_TEST_SQLSERVER_CONNECTION='<dedicated test database connection>'
dotnet test ServiceForge.slnx --no-restore --no-build
dotnet list ServiceForge.slnx package --include-transitive --vulnerable
git diff --check
```

The test suite uses xUnit, WebApplicationFactory, and real SQL Server. EF Core InMemory and SQLite
are not evidence of SQL Server concurrency correctness. Docker is not required by the repository's
verified local test path and is intentionally not configured here.

## Platform and delivery notes

The .NET solution, SQL Server provider, build, unit tests, and Windows PowerShell workflow were
verified in the repository environment. macOS/Linux execution of the complete test suite is
unverified. SQL Server connectivity, authentication, and time-zone identifier behavior may require
OS-specific setup; the seeded identifiers include IANA names, while some DST unit coverage uses a
Windows time-zone identifier. Treat that as a setup/platform review item rather than an assurance of
cross-platform compatibility.

The approved acceptance matrix in `docs/acceptance-traceability.md` records all 24 mandatory
scenarios as covered. T036–T037 are documentation and delivery-readiness tasks; the retrospective
AI decision/assumption audit and Batch R1 corrections are recorded in `docs/ai-development-log.md`.

## AI Collaboration Narrative

ServiceForge follows specification-driven development: the ratified constitution, approved feature
specification, architecture plan, data model, API contract, ADRs, and task list define the work.
The owner approved the architectural decisions and bounded implementation batches before coding.

Codex was used for bounded, reviewable tasks. Generated changes were checked against the approved
artifacts, reviewed for architecture and security, and validated with executed unit, HTTP, and real
SQL Server integration tests. The retrospective submission audit identified four corrective gaps;
Batch R1 addressed stale post-lock time, permissive timestamp parsing, governance wording, and
OpenAPI execution coverage, with results recorded in `docs/ai-development-log.md`.

The owner and reviewers retain responsibility for final engineering quality and approval. AI
assistance does not replace requirements ownership, code review, security review, or executed test
evidence. Known limitations, including unverified complete macOS/Linux execution and the absence of
an induced SQL deadlock test, remain explicitly documented.
