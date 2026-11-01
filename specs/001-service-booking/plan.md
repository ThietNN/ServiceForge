# Implementation Plan: Service Booking

**Branch**: `001-service-booking` | **Date**: 2026-11-01 | **Spec**: [spec.md](./spec.md)

**Input**: Approved feature specification from `specs/001-service-booking/spec.md`.

## Summary

ServiceForge is a backend-only ASP.NET Core Web API for P0 automotive service booking across
multiple dealerships. A customer books an owned vehicle, dealership, service type, and explicit-offset
start time. The system validates the local operating window and elapsed duration, automatically assigns
one eligible technician and one compatible bay, prevents vehicle and resource conflicts, and provides
durable global idempotency for retries.

The implementation uses four Clean Architecture projects. Application commands and queries coordinate
domain rules and persistence through MediatR. SQL Server transaction protection uses READ COMMITTED,
`READ_COMMITTED_SNAPSHOT OFF`, transaction-owned exclusive `sp_getapplock` locks, and the approved
Idempotency Key → Vehicle → Dealership lock order. EF Core DbContext is the Unit of Work; focused
repository abstractions are introduced only where they make the booking workflow or query boundaries
clear.

## Technical Context

**Language/Version**: C# on ASP.NET Core targeting `net10.0`. The environment provides SDK `10.0.403`
and ASP.NET Core runtime `10.0.14`; .NET 10 is an active LTS release under Microsoft's support policy.
Exact package versions remain an implementation-time compatibility check.

**Primary Dependencies**: EF Core with SQL Server, MediatR, FluentValidation, built-in dependency
injection, Swagger/OpenAPI, and structured `ILogger` logging.

**Storage**: SQL Server with EF Core; UTC `datetime2(7)` timestamps and the ten-table model in
`data-model.md`.

**Testing**: xUnit, `WebApplicationFactory`, real SQL Server through Testcontainers, separate
connections and controlled synchronization for concurrency tests.

**Target Platform**: Backend-only ASP.NET Core Web API. No Angular, Playwright, microservices, or
message queues.

**Project Type**: Backend web service using Clean Architecture and CQRS.

**Performance Goals**: No numeric throughput or latency target is specified by the approved
requirements. The implementation must preserve the specified five-second lock-acquisition timeout and
the approved pagination behavior; no additional target is invented here.

**Constraints**: P0 creates only fully assigned Confirmed appointments and read operations. Booking
transactions use SQL Server READ COMMITTED with `READ_COMMITTED_SNAPSHOT OFF`, transaction-owned
exclusive `sp_getapplock`, strict global lock ordering, five-second per-lock timeout, atomic appointment
and successful idempotency persistence, no automatic server-side transaction retries, and no separate
committed Processing state.

**Scale/Scope**: P0 automotive service booking across multiple dealerships with the five approved API
endpoints, offset pagination, global idempotency keys retained for 24 hours, and the ten approved tables.
No additional scale assumption is made.

## Constitution Check

*GATE: Must pass before design work and re-check after the design below.*

| Constitutional gate | Result | Evidence in this plan |
|---|---|---|
| Approved specifications are authoritative | PASS | Every architecture, workflow, schema, endpoint, and exclusion is traced to the approved constitution, `spec.md`, or this explicitly approved T002C instruction. |
| Clean Architecture and dependency direction | PASS | Four projects are defined with inward dependencies only; Domain has no Infrastructure or Presentation dependency. |
| Approved technology boundaries | PASS | ASP.NET Core, C#, SQL Server, EF Core, MediatR, built-in DI, FluentValidation, Swagger/OpenAPI, and structured logging are used; Angular, Playwright, microservices, and queues are excluded. |
| Business integrity | PASS | Ownership, dealership boundaries, resource eligibility, half-open conflicts, UTC/local-time rules, and P0/P1 boundaries are preserved. |
| Transaction and concurrency | PASS | The workflow uses READ COMMITTED, `READ_COMMITTED_SNAPSHOT OFF`, transaction-owned `sp_getapplock`, the approved lock order, five-second timeouts, revalidation, atomic persistence, and no automatic transaction retry. |
| API and data | PASS | The five approved endpoints, pagination, Problem Details mappings, UTC behavior, and required idempotency key are preserved. |
| Quality | PASS | Unit, API integration, real SQL Server Testcontainers, concurrency, rollback, timezone, ownership, allocation, and HTTP contract testing are planned. |
| Authorization | PASS | This plan creates documentation only. It does not authorize code, migrations, Docker configuration, or runtime infrastructure. |

**Post-design gate**: PASS. The data model and workflow below do not add a DealershipServiceType
relationship, a committed Processing state, an overlap trigger, or any P1 workflow. Nullable resource
columns and selected lifecycle values are documented as P1 schema anticipation only.

## Architecture and Dependency Direction

The solution is planned as four projects:

```text
ServiceForge.Api
    -> ServiceForge.Application
        -> ServiceForge.Domain
    -> ServiceForge.Infrastructure
        -> ServiceForge.Application
            -> ServiceForge.Domain
ServiceForge.Application -> ServiceForge.Domain
ServiceForge.Infrastructure -> ServiceForge.Domain
```

The API references Application. Infrastructure is composed by the API at the application boundary and
implements Application abstractions; it does not become a dependency of Domain. Application references
Domain and contains use-case orchestration without depending on presentation or infrastructure details.

### ServiceForge.Domain

- Defines appointment, customer, vehicle, dealership, service type, technician, bay, and idempotency
  domain concepts and invariants that are independent of storage or HTTP.
- Represents approved statuses and assignment states, including the P0 rule that newly created
  appointments are Confirmed and Assigned.
- Defines focused domain abstractions only when they express a domain boundary; it does not reference
  EF Core, SQL Server, MediatR, ASP.NET Core, or logging infrastructure.
- Does not own HTTP error mapping, database transactions, SQL application locks, or serialization.

### ServiceForge.Application

- Contains CQRS commands, queries, handlers, validation rules, response models, and use-case
  orchestration.
- Uses MediatR to dispatch the appointment creation command and read queries.
- Uses FluentValidation for syntactic and request-level validation, including required fields,
  explicit-offset input, whole-minute input, required `Idempotency-Key`, and pagination bounds.
- Coordinates the booking workflow while requiring mutable business facts to be revalidated after
  transaction locks are acquired.
- Defines focused persistence and clock abstractions where justified, without exposing EF Core or
  SQL Server types to Domain.
- Maps approved business outcomes to RFC 9457 Problem Details codes at the API boundary through
  application result/error information.

### ServiceForge.Infrastructure

- Implements EF Core DbContext as the Unit of Work and maps the ten approved tables, keys, indexes,
  foreign keys, checks, and no-cascade-delete behavior.
- Implements SQL Server transaction handling, transaction-owned exclusive `sp_getapplock`, canonical
  request hashing, UTC clock access, idempotency persistence, and resource/appointment queries.
- Provides repository abstractions only for focused boundaries justified by the booking workflow; it
  does not introduce a generic repository layer by default.
- Provides reference-data seeding for the demonstration without adding a DealershipServiceType table.
- Contains SQL Server-specific behavior and Testcontainers test support; no SQL overlap trigger is
  planned for P0.

### ServiceForge.Api

- Hosts the ASP.NET Core Web API and exposes only the approved P0 endpoints.
- Configures built-in dependency injection, MediatR, FluentValidation, EF Core, Swagger/OpenAPI, and
  structured `ILogger` logging.
- Translates application outcomes into HTTP status codes, Location headers, UTC representations, and
  RFC 9457 Problem Details with machine-readable business codes.
- Does not contain booking business rules, direct database access, or resource-selection logic.

## Booking Workflow

The following is the single approved booking workflow. Early request validation may occur before lock
acquisition, but any mutable business state relied upon for booking is revalidated under transaction
protection. There is no second transaction and no independently committed Processing record.

1. The API receives `POST /api/appointments`, parses the request, validates required fields and the
   required `Idempotency-Key`, and rejects invalid syntax or shape using the approved error contract.
2. The application validates that the supplied start time is ISO 8601 with an explicit UTC offset and
   normalizes the represented instant to UTC. The supplied offset is authoritative for the instant; the
   dealership timezone is used later for local business validation.
3. The application computes the canonical typed request identity from customerId, vehicleId,
   dealershipId, serviceTypeId, and normalized UTC start instant. JSON property order does not affect
   the identity. It computes the SHA-256 request hash from that canonical identity.
4. The application begins one SQL Server transaction using READ COMMITTED isolation with
   `READ_COMMITTED_SNAPSHOT OFF`.
5. Within that transaction, it acquires the transaction-owned exclusive idempotency-key
   `sp_getapplock` first. This is the first lock in the strict global order: Idempotency Key → Vehicle
   → Dealership.
6. The idempotency-key lock uses a five-second timeout. Failure to acquire it returns HTTP 409
   `REQUEST_IN_PROGRESS` with `Retry-After: 2`.
7. While holding the idempotency-key lock, the application checks the global idempotency record for the
   supplied key and determines whether it is unexpired using SQL Server UTC as the authoritative clock.
8. If an unexpired record has the same canonical request hash, the application returns the saved
   successful response immediately without acquiring the vehicle or dealership locks and without
   creating another appointment.
9. If an unexpired record has a different canonical request hash, the application returns
   `IDEMPOTENCY_KEY_CONFLICT` without acquiring the vehicle or dealership locks.
10. For a new or expired request, the application acquires the transaction-owned exclusive vehicle
    `sp_getapplock`, then the dealership `sp_getapplock`, preserving the strict Idempotency Key →
    Vehicle → Dealership order. Each remaining lock uses a five-second timeout. Failure to acquire
    either returns HTTP 503 `BOOKING_TEMPORARILY_UNAVAILABLE` with `Retry-After: 2`; a SQL deadlock
    has the same result.
11. After the vehicle and dealership locks are acquired, the application revalidates customer,
    vehicle, dealership, service type, ownership, dealership-local operating hours, inclusive
    one-local-calendar-year horizon, whole-minute start, strictly-future start, positive duration,
    and elapsed-time end instant.
12. The application revalidates global vehicle conflicts using half-open intervals `[start, end)`;
    overlap exists when `existing.Start < requested.End` and `existing.End > requested.Start`.
    Vehicle checks span all dealerships.
13. The application revalidates resource eligibility and availability: the technician must be active,
    belong to the requested dealership, and be qualified; the bay must be active, belong to the
    requested dealership, and be compatible. Availability must cover the entire interval.
14. The application independently selects the lowest-ID available eligible technician and the
    lowest-ID available eligible bay. If a low-ID resource is occupied, it considers the next eligible
    available resource.
15. The application converts start and end to dealership-local time for operating-hours and daylight-
    saving validation, while calculating elapsed duration from the UTC start instant and snapshotting
    the service duration on the appointment.
16. The application persists one fully assigned Confirmed appointment and the successful idempotency
    response in the same transaction. P0 does not persist a separate Processing state.
17. If the idempotency key was expired, replacement is performed atomically in the same transaction.
    The original appointment is not deleted or modified; if replacement fails, rollback preserves the
    previous idempotency record and appointment.
18. The application commits the transaction and the API returns HTTP 201 Created, a Location header,
    the confirmed appointment representation, assigned resource IDs, and UTC timestamps.
19. If commit outcome is uncertain, the server does not assume rollback and does not automatically
    retry. The client can retry with the original key to recover the saved successful result.

## Time, Conflict, and Idempotency Rules

### Time

- Dealership-local operating hours are Monday–Friday, 09:00–17:00; weekends are closed.
- The entire elapsed appointment must fit within one local operating day; ending exactly at 17:00 is
  valid.
- Start is strictly later than SQL Server current UTC time, with no rounding.
- The inclusive horizon is one dealership-local calendar year based on date; February 29 maps to
  February 28 when the following year lacks February 29.
- Service duration is positive and represents elapsed real time. End is calculated by adding duration
  to UTC start, then both instants are converted to local time for business validation.
- Start and end are persisted and returned in UTC. No holiday calendar or minimum advance notice is
  introduced in P0.

### Conflicts and Serialization

- All appointment intervals are half-open `[start, end)`; exact end-to-start adjacency is permitted.
- Vehicle conflict checks are global across dealerships.
- The vehicle lock serializes changes involving a vehicle; the dealership lock serializes resource
  assignment within the dealership. The idempotency lock serializes requests sharing a key.
- The locks protect integrity only when every booking writer follows the protocol. Direct database
  writes that bypass application locks are an explicit limitation; no overlap trigger is planned for P0.

### Idempotency

- The key is global, required on booking creation, retained for 24 hours from creation, and has no
  Status column.
- The idempotency-key lock is acquired and the existing unexpired record is checked before vehicle or
  dealership locks. Matching requests replay the stored successful response without acquiring those
  additional locks; mismatched requests return `IDEMPOTENCY_KEY_CONFLICT` without acquiring them.
- New and expired requests acquire the remaining locks in order: Vehicle, then Dealership.
- Request identity uses typed values and normalized UTC instant, so JSON property order and equivalent
  offset representations do not change identity.
- Expired-key replacement must preserve the original appointment and previous record if the replacement
  transaction fails.

## API, Pagination, and Error Contracts

The API exposes these P0 endpoints:

| Method | Endpoint | Behavior |
|---|---|---|
| POST | `/api/appointments` | Creates a fully assigned Confirmed appointment; returns 201 Created and Location. |
| GET | `/api/appointments/{id}` | Returns 200 when found and 404 when absent. |
| GET | `/api/customers/{id}/vehicles` | Returns customer vehicles using offset pagination. |
| GET | `/api/dealerships` | Returns dealerships using offset pagination. |
| GET | `/api/service-types` | Returns service types using offset pagination. |

Collection responses contain `items`, `page`, `pageSize`, `totalCount`, and `totalPages`. Page is
1-based and defaults to 1; pageSize defaults to 20 and has a maximum of 100. Ordering is stable by ID
ascending and pagination is applied in the database.

All errors use RFC 9457 Problem Details with a machine-readable business code and field-level details
where applicable. The approved mappings are:

| Status | Codes |
|---|---|
| 400 | `INVALID_REQUEST`, `INVALID_BOOKING_TIME`, `OUTSIDE_OPERATING_HOURS`, `BOOKING_HORIZON_EXCEEDED` |
| 403 | `VEHICLE_NOT_OWNED` |
| 404 | `RESOURCE_NOT_FOUND` |
| 409 | `VEHICLE_TIME_CONFLICT`, `NO_QUALIFIED_TECHNICIAN`, `NO_COMPATIBLE_BAY`, `NO_AVAILABLE_RESOURCES`, `IDEMPOTENCY_KEY_CONFLICT`, `REQUEST_IN_PROGRESS` |
| 503 | `BOOKING_TEMPORARILY_UNAVAILABLE` |

`REQUEST_IN_PROGRESS` and `BOOKING_TEMPORARILY_UNAVAILABLE` include `Retry-After: 2` seconds.

## Test Strategy

- Unit tests cover deterministic time, duration, interval, canonical identity, validation, resource
  eligibility, error mapping, and pagination rules.
- API integration tests use xUnit and `WebApplicationFactory` to verify endpoint behavior, HTTP status
  codes, Location headers, Problem Details, UTC representations, and Swagger/OpenAPI documentation.
- Database integration tests use real SQL Server through Testcontainers; EF Core InMemory is not proof
  of SQL Server concurrency correctness.
- Concurrency tests use separate database connections and controlled synchronization to verify vehicle
  conflicts, resource allocation, idempotency-key contention, lock timeout mapping, deadlock mapping,
  atomic rollback, and lost-response retry behavior.
- Timezone tests cover explicit offsets, local operating hours, whole-minute boundaries, 17:00 end,
  inclusive annual horizon, February 29, daylight-saving transitions, and elapsed-time duration.
- Ownership and allocation tests cover cross-dealership vehicle checks, dealership boundaries,
  qualification and compatibility, lowest-ID selection, occupied-resource fallback, and all approved
  resource errors.
- Tests must be executed before reporting success; no unexecuted test result may be claimed as passing.

## Implementation Sequencing

1. Establish the four-project solution boundaries and dependency direction.
2. Define Domain concepts, approved status values, interval rules, and pure time/identity rules.
3. Define Application commands, queries, validators, handlers, result types, and focused abstractions.
4. Define the EF Core Code First model, ten tables, constraints, indexes, no-cascade-delete behavior,
   reviewed migrations, and demonstration reference-data seed. Every migration requires review before
   use; migrations are not generated by this documentation task.
5. Implement SQL Server clock, canonical hash, transaction, lock protocol, idempotency, conflict
   queries, resource selection, and atomic persistence in Infrastructure.
6. Implement API endpoints, Problem Details mapping, pagination, Location behavior, Swagger/OpenAPI,
   and structured logging.
7. Add deterministic unit tests and API integration tests.
8. Add Testcontainers SQL Server integration and controlled concurrency tests.
9. Execute the complete acceptance scenario set and document known limitations.

No step authorizes implementation in this documentation task; implementation requires explicit
authorization and must remain traceable to the approved specification and tasks.

## Architectural Tradeoffs and Limitations

- SQL application locks provide an explicit serialization protocol for booking writers and avoid
  relying on a database overlap trigger that is not planned for P0. The tradeoff is that direct writes
  or future writers that bypass the protocol can violate the intended integrity guarantees.
- Dealership-level serialization simplifies consistent resource assignment and protects the approved
  allocation workflow, at the cost of reducing parallelism for bookings in the same dealership.
- The global lock order prevents lock-order inversions across idempotency, vehicle, and dealership
  resources, while five-second acquisition timeouts produce explicit transient outcomes.
- A single transaction atomically persists the appointment and successful idempotency response. The
  tradeoff is that an uncertain commit outcome cannot be classified as rollback; the client retry path
  is therefore part of the contract.
- EF Core DbContext as Unit of Work keeps persistence atomic within the application boundary. Focused
  repositories are used only where they clarify a boundary; a generic repository abstraction is not
  introduced without approval.
- Nullable appointment resource IDs and selected lifecycle values anticipate P1 schema needs. P0 still
  creates only fully assigned Confirmed appointments and does not implement P1 lifecycle workflows.
- SQL Server UTC is authoritative for future-time and idempotency-expiration decisions. Dealership
  timezones remain authoritative for local operating-hour and horizon validation.
- No numeric throughput or latency target is defined by the approved requirements, so performance
  tuning targets remain an unresolved future decision rather than an invented requirement.
- `net10.0` is selected from the inspected environment and current official support status; package
  compatibility must still be verified when dependencies are selected.

## Project Structure

### Documentation (this feature)

```text
specs/001-service-booking/
├── spec.md
├── checklists/
│   └── requirements.md
├── plan.md
├── data-model.md
└── contracts/
    └── .gitkeep
```

No research, quickstart, API contract, migration, or task artifact is created by T002C beyond the
approved deliverables listed above.

### Source Code (repository root; planned only)

```text
src/
├── ServiceForge.Api/
├── ServiceForge.Application/
├── ServiceForge.Domain/
└── ServiceForge.Infrastructure/

tests/
├── ServiceForge.UnitTests/
├── ServiceForge.Api.IntegrationTests/
└── ServiceForge.Infrastructure.IntegrationTests/
```

**Structure Decision**: Use four Clean Architecture production projects with inward dependency
direction from API and Infrastructure toward Application and Domain. Keep SQL Server and EF Core
concerns in Infrastructure, use Application for CQRS orchestration and validation, keep Domain
framework-independent, and expose the approved REST endpoints through Api. The source and test
directories above are planned documentation only and are not created by T002C.

## Complexity Tracking

No constitutional violations require justification. The four-project structure and focused repository
abstractions are explicitly approved architecture decisions for this feature.
