---

description: "Approved implementation task breakdown for ServiceForge P0 service booking"
---

# Tasks: Service Booking

**Input**: Approved design documents from `specs/001-service-booking/`

**Prerequisites**: `spec.md`, `plan.md`, `data-model.md`, `contracts/api.md`, requirements checklist,
and ADRs 001–007.

**Scope**: Backend-only P0 automotive service booking. T005 records the authorization gate; the
current user authorization covers the T028-T031 HTTP API batch.

**Task format**: Every task includes its ID, dependencies, relevant paths or artifacts, verification
criteria, and explicit P0 boundary.

## Phase A: Documentation and planning

**Purpose**: Record the approved specification baseline and establish an explicit authorization gate.

- [x] T001 Bootstrap the Spec Kit workspace and Codex integration in `.specify/` and `.agents/skills/`; depends on repository bootstrap authorization; verify official Spec Kit structure and Codex skills exist; P0 boundary: documentation infrastructure only, no application code or dependencies.
- [x] T002 Complete the approved constitution, feature specification, architecture plan, ten-table data model, API contract, and ADRs in `.specify/memory/constitution.md`, `specs/001-service-booking/`, and `docs/adr/`; depends on T001; verify approved artifacts are internally consistent; P0 boundary: approved P0 documentation only, with P1 workflows deferred.
- [x] T003 Finalize this dependency-ordered task breakdown in `specs/001-service-booking/tasks.md`; depends on T002; verify exactly T001–T037 are present with no duplicate or missing IDs; P0 boundary: planning only and no authorization implied.
- [x] T004 Verify documentation consistency and implementation readiness across `AGENTS.md`, `.specify/memory/constitution.md`, `specs/001-service-booking/`, and `docs/adr/`; depends on T003; verify approved requirements, contracts, schema, ADRs, and acceptance coverage agree; P0 boundary: no source, migration, Docker, or runtime changes.
- [x] T005 Establish the implementation authorization checkpoint in `docs/ai-development-log.md` and the Git review record; depends on T004 and explicit human authorization; verify authorization is recorded before any implementation task starts; P0 boundary: authorization is recorded for the approved T006-T010 batch only.

## Phase B: Solution foundation

**Purpose**: Create the approved .NET solution boundaries and shared application foundation after T005 authorization.

- [x] T006 Create `ServiceForge.slnx` and the four `src/ServiceForge.Api/`, `src/ServiceForge.Application/`, `src/ServiceForge.Domain/`, and `src/ServiceForge.Infrastructure/` .NET 10 production projects; depends on T005; verify all projects target `net10.0` and no frontend project exists; P0 boundary: exactly four approved production projects, no Angular, Playwright, microservices, or message queues.
- [x] T007 Configure project references and built-in dependency injection in `src/ServiceForge.slnx`, `src/ServiceForge.Api/Program.cs`, and project files; depends on T006; verify dependencies point inward, Domain has no Infrastructure/Presentation reference, and Infrastructure composition occurs at the API boundary; P0 boundary: preserve Clean Architecture and do not add unapproved frameworks.
- [x] T008 Establish approved package dependencies and configuration in `src/ServiceForge.Api/*.csproj`, `src/ServiceForge.Application/*.csproj`, `src/ServiceForge.Infrastructure/*.csproj`, and `src/ServiceForge.Api/appsettings*.json`; depends on T007; verify EF Core SQL Server, MediatR, FluentValidation, Swagger/OpenAPI, structured logging, xUnit/WebApplicationFactory, and approved SQL Server integration testing are configured without unrelated dependencies; P0 boundary: no authentication/JWT, Angular, Playwright, or runtime infrastructure. Batch foundation packages and xUnit tests are configured; WebApplicationFactory remains scheduled for later integration tasks.
- [x] T009 [P] Define Domain entities, value objects, and approved enums in `src/ServiceForge.Domain/Entities/`, `src/ServiceForge.Domain/ValueObjects/`, and `src/ServiceForge.Domain/Enums/`; depends on T007; verify appointment/resource/status invariants and framework independence; P0 boundary: model only approved P0 behavior plus explicitly approved P1 schema anticipation.
- [x] T010 Define Application CQRS contracts and validation in `src/ServiceForge.Application/Common/`, `src/ServiceForge.Application/Appointments/`, and `src/ServiceForge.Application/ReferenceData/`; depends on T007 and T009; verify MediatR commands/queries, FluentValidation rules, result/error contracts, and focused abstractions are traceable to `spec.md` and `contracts/api.md`; P0 boundary: no generic repository or unapproved use case.

## Phase C: SQL Server persistence

**Purpose**: Implement and verify the approved ten-table SQL Server model using EF Core Code First.

- [x] T011 Implement the EF Core DbContext and entity mappings in `src/ServiceForge.Infrastructure/Persistence/ServiceForgeDbContext.cs` and `src/ServiceForge.Infrastructure/Persistence/Configurations/`; depends on T008 and T009; verify exactly Customer, Vehicle, Dealership, ServiceType, Technician, ServiceBay, TechnicianQualification, BayServiceCompatibility, Appointment, and IdempotencyRecord are mapped; P0 boundary: no DealershipServiceType table and no extra persistence table.
- [x] T012 Implement composite foreign keys, checks, indexes, and non-cascading relationships in `src/ServiceForge.Infrastructure/Persistence/Configurations/`; depends on T011; verify `(VehicleId, CustomerId)`, `(TechnicianId, DealershipId)`, `(ServiceBayId, DealershipId)`, interval/duration/status/assignment/completion checks, conflict-search indexes, and no cascade deletes; P0 boundary: no overlap trigger or exclusion constraint.
- [x] T013 Generate and review EF Core Code First migrations in `src/ServiceForge.Infrastructure/Persistence/Migrations/`; depends on T012 and T005; verify migration SQL matches the approved ten-table model and receives human review before use; P0 boundary: migrations are reviewed artifacts, not an authorization to expand schema or implement P1 workflows.
- [x] T014 [P] Implement approved reference-data seeding in `src/ServiceForge.Infrastructure/Persistence/Seed/`; depends on T011 and T012; verify seeded customers, vehicles, dealerships/timezones, service types/durations, technicians, bays, qualifications, and compatibilities support demonstration scenarios; P0 boundary: seed only approved demonstration data and no direct dealership-service catalog.
- [x] T015 [P] Establish dedicated local SQL Server integration infrastructure and schema validation in `tests/ServiceForge.Infrastructure.IntegrationTests/`; depends on T008 and T012; verify real SQL Server connectivity through `SERVICEFORGE_TEST_SQLSERVER_CONNECTION`, schema application, ten-table count, constraints, indexes, READ_COMMITTED_SNAPSHOT OFF, and clean test isolation; P0 boundary: EF Core InMemory and SQLite are not proof of SQL Server behavior.

## Phase D: Booking business rules

**Purpose**: Implement deterministic P0 booking validation, ownership, conflict, eligibility, and selection rules.

- [x] T016 [P] Implement canonical booking-time normalization and validation in `src/ServiceForge.Application/Appointments/Validation/` and `src/ServiceForge.Domain/Time/`; depends on T010 and T011; verify explicit ISO 8601 offset, whole-minute start, strictly-future SQL Server UTC comparison, positive duration, elapsed UTC end calculation, and duration snapshot rules; Coverage: US1/US3; P0 boundary: no holiday calendar or minimum advance notice.
- [x] T017 [P] Implement dealership-local operating-hours and calendar-year horizon rules in `src/ServiceForge.Application/Appointments/Validation/` and `src/ServiceForge.Domain/Time/`; depends on T016 and dealership timezone mapping from T011; verify Monday–Friday 09:00–17:00, weekend closure, exact 17:00 end, inclusive one-local-year horizon, February 29 fallback, and DST behavior; Coverage: US1/US3; P0 boundary: no holidays or P1 scheduling workflows.
- [x] T018 [P] Implement vehicle ownership and cross-dealership overlap checks in `src/ServiceForge.Application/Appointments/Rules/` and `src/ServiceForge.Infrastructure/Persistence/Queries/`; depends on T012 and T016; verify one-owner vehicle enforcement, customer ownership validation, half-open overlap detection, and global vehicle checks; Coverage: US1/US4; P0 boundary: no travel time or resource buffers.
- [x] T019 [P] Implement technician qualification, bay compatibility, and availability queries in `src/ServiceForge.Infrastructure/Persistence/Queries/` and `src/ServiceForge.Application/Appointments/Rules/`; depends on T012, T016, and T017; verify active dealership ownership, many-to-many qualification/compatibility, full-interval availability, and approved no-resource errors; Coverage: US2; P0 boundary: no DealershipServiceType relationship or reassignment workflow.
- [x] T020 Implement deterministic lowest-ID resource selection in `src/ServiceForge.Application/Appointments/Allocation/`; depends on T019; verify technician and bay are selected independently, occupied low-ID resources fall through to the next available eligible resource, and both resources are required for P0 assignment; Coverage: US1/US2; P0 boundary: customers never choose resources and no scheduling optimization is added.

## Phase E: Transaction and idempotency

**Purpose**: Implement the approved SQL Server transaction protocol, idempotency behavior, atomic persistence, and reliability tests.

- [x] T021 [P] Implement canonical typed SHA-256 request hashing in `src/ServiceForge.Application/Appointments/Idempotency/`; depends on T010 and T016; verified typed customerId/vehicleId/dealershipId/serviceTypeId plus normalized UTC start identity, property-order independence, and equivalent-offset identity with focused unit tests; Coverage: US5; P0 boundary: global key scope only and no Processing state.
- [x] T022 [P] Implement the SQL Server authoritative UTC clock in `src/ServiceForge.Infrastructure/Time/`; depends on T008 and T011; verified `SYSUTCDATETIME()` execution on the active SQL connection and UTC result semantics with local SQL Server integration tests; Coverage: US3/US5; P0 boundary: no application-local clock authority.
- [x] T023 Implement transaction-owned SQL Server application locks in `src/ServiceForge.Infrastructure/Persistence/Transactions/`; depends on T011, T021, and T022; verified READ COMMITTED transactions, transaction-owned exclusive locks, five-second lock timeout, deterministic parameterized resources, return-code handling, release, and no automatic retries; the approved Idempotency Key → Vehicle → Dealership order remains the caller protocol; Coverage: US4/US5; P0 boundary: direct database writers bypassing the protocol remain a documented limitation.
- [x] T024 Implement idempotency replay, conflict, and expiry behavior in `src/ServiceForge.Application/Appointments/Idempotency/` and `src/ServiceForge.Infrastructure/Persistence/`; depends on T021, T022, and T023; verified matching replay, mismatched conflict, expired-key reuse, 24-hour response retention, and no modification of the original appointment; lookup remains a separate layer after the idempotency lock and before unnecessary resource locks; Coverage: US5; P0 boundary: no Processing state.
- [x] T025 Implement atomic appointment and idempotency persistence in `src/ServiceForge.Infrastructure/Persistence/` and `src/ServiceForge.Application/`; depends on T018, T020, T023, and T024; verified one fully assigned Confirmed appointment and successful idempotency response commit together, rollback, expired replacement preservation, and uncertain commit outcomes remain uncertain; Coverage: US1/US4/US5; P0 boundary: no separately committed Processing record.
- [x] T026 Implement booking command orchestration and approved error mapping in `src/ServiceForge.Application/Appointments/CreateAppointment/`; depends on T016–T025; verified request validation, canonical identity, SQL-time validation, ordered locks, immediate idempotency lookup/replay/conflict, locked mutable-state revalidation, deterministic allocation, atomic persistence, approved typed outcomes, and transient lock/deadlock classification; Coverage: US1–US5; P0 boundary: no HTTP endpoints or new business codes.
- [x] T027 Implement controlled concurrency and rollback tests in `tests/ServiceForge.Infrastructure.IntegrationTests/`; depends on T015 and T023–T026; verified separate connections/scopes cover same-key contention/conflict, cross-dealership vehicle serialization, resource allocation, replay lock avoidance, lock timeouts, atomic rollback, expired-key replacement, and persisted database state using real SQL Server; deadlock classification is implemented but no artificial deadlock was induced; Coverage: US4/US5; P0 boundary: no API endpoints, Docker, Testcontainers, or P1 behavior.

## Phase F: API endpoints

**Purpose**: Expose and verify only the approved P0 REST endpoints.

- [x] T028 Implement the approved `POST /api/appointments` route in `src/ServiceForge.Api/Endpoints/`; depends on T026 and T027; verified required `Idempotency-Key`, request binding, 201 Created, Location, Confirmed/Assigned representation, assigned IDs, UTC timestamps, replay, and conflict behavior; Coverage: US1/US5; P0 boundary: no authentication/JWT or additional write endpoint.
- [x] T029 Implement `GET /api/appointments/{id}` in `src/ServiceForge.Api/Endpoints/`; depends on T010, T011, and T026; verified 200 representation for an existing appointment and 404 `RESOURCE_NOT_FOUND` when absent; Coverage: US6; P0 boundary: no cancellation, completion, or rescheduling endpoint.
- [x] T030 Implement paginated `GET /api/customers/{id}/vehicles`, `GET /api/dealerships`, and `GET /api/service-types` in `src/ServiceForge.Api/Endpoints/` and SQL-backed readers; depends on T010, T011, and T014; verified default page 1/pageSize 20, bounds, stable ascending ID ordering, SQL Skip/Take pagination, and required metadata; Coverage: US6; P0 boundary: read-only reference endpoints only.
- [x] T031 Implement RFC 9457-compatible Problem Details, OpenAPI, and HTTP contract verification in `src/ServiceForge.Api/Endpoints/` and `tests/ServiceForge.Api.IntegrationTests/`; depends on T026 and T028–T030; verified approved business codes/statuses, field validation details, and endpoint contract behavior. Retry-After mapping remains implemented for transient outcomes; no unapproved contract surface was added; Coverage: US1–US6; P0 boundary: no authentication or UI contract.

## Phase G: Quality and delivery

**Purpose**: Complete reviewable tests, acceptance verification, documentation, and delivery readiness.

- [ ] T032 Implement deterministic Domain and Application unit tests in `tests/ServiceForge.UnitTests/`; depends on T009, T010, T016–T020, and T021; verify time, duration, intervals, ownership, allocation, canonical identity, validation, error mapping, and pagination rules; Coverage: US1–US6; P0 boundary: no claim of passing tests without execution.
- [ ] T033 Implement API integration tests using `WebApplicationFactory` in `tests/ServiceForge.Api.IntegrationTests/`; depends on T028–T031 and T015; verify all five endpoints, HTTP contracts, Problem Details, Location header, UTC response data, pagination, and P0 exclusions; Coverage: US1/US5/US6; P0 boundary: no authentication/JWT tests because authentication is excluded from P0.
- [ ] T034 Verify all 24 mandatory acceptance scenarios in `tests/ServiceForge.UnitTests/`, `tests/ServiceForge.Api.IntegrationTests/`, and `tests/ServiceForge.Infrastructure.IntegrationTests/`; depends on T027, T032, and T033; verify successful assignment, resource fallback/errors, ownership, cross-dealership conflicts, adjacency, time boundaries, horizon/DST, concurrency, idempotency, rollback, pagination, and Problem Details; Coverage: US1–US6; P0 boundary: do not add P1 lifecycle scenarios.
- [ ] T035 Execute the full regression suite and correct failures in the affected `src/` and `tests/` paths; depends on T034; verify the complete suite actually runs and all corrections remain traceable to approved requirements; P0 boundary: do not weaken tests, bypass SQL Server verification, or introduce unapproved behavior.
- [ ] T036 Write README, setup instructions, and architectural tradeoffs in `README.md` and `docs/`; depends on T035; verify setup documents `net10.0`, dedicated local SQL Server prerequisites, Spec Kit feature context, lock/idempotency limitations, and no unauthorized infrastructure assumptions; P0 boundary: documentation only and no Docker configuration unless separately authorized.
- [ ] T037 Complete final submission readiness review and Git checkpoint in `docs/ai-development-log.md`, the approved artifacts, and Git metadata; depends on T005 and T036; verify requirements traceability, executed verification evidence, clean diff, no unexpected files, and explicit implementation authorization status before release; P0 boundary: do not claim completion or authorization without human approval.

## Dependencies and Execution Order

### Phase dependencies

- Phase A documentation tasks T001–T004 are complete; T005 is the completed human authorization gate.
- Phase B tasks T006–T010 depend on T005 and block all application implementation work that follows.
- Phase C tasks depend on the solution foundation; T011–T012 block persistence-dependent work, while
  T014 and T015 can proceed after their stated prerequisites.
- Phase D tasks depend on the Domain/Application/persistence foundations and provide the deterministic
  rules used by booking orchestration.
- Phase E depends on the business rules and persistence; T023–T027 establish the transaction and
  idempotency integrity boundary before the POST endpoint is exposed.
- Phase F depends on the Application workflow and persistence; all endpoint work remains P0-only.
- Phase G depends on the implemented endpoints and test infrastructure; T037 is the final checkpoint.

### Dependency graph

```text
T001 → T002 → T003 → T004 → T005
                         └→ T006 → T007 → T008 → T009 → T010
                                      ├→ T011 → T012 → T013
                                      │          ├→ T014
                                      │          └→ T015
                                      └→ T016 → T017
                                           ├→ T018 → T019 → T020
                                           └→ T021 → T022 → T023 → T024 → T025 → T026 → T027
                                                                           └→ T028 → T029/T030 → T031
                                                                                      └→ T032/T033 → T034 → T035 → T036 → T037
```

### Acyclic execution order

Tasks are sequentially numbered in dependency order. `[P]` marks only tasks that can work in parallel
after their listed prerequisites without editing the same primary artifacts. No task may bypass T005.

## Parallel Execution Opportunities

- After T007, T009 can proceed independently of T008’s package/configuration work.
- After T011–T012, T014 reference-data seeding and T015 local SQL Server schema validation can proceed
  in parallel.
- After T016, T017 and T018 can proceed in parallel; T019 follows the persistence/resource foundation.
- After T021, T022 can proceed in parallel before T023 combines clock, hash, and persistence behavior.
- After the foundational booking workflow is complete, unit tests, API integration tests, and
  acceptance-scenario verification can be divided by test project, subject to shared-file conflicts.
- Endpoint implementation T029 and T030 can proceed in parallel after their shared Application and
  persistence prerequisites.

## User Story Coverage and Independent Tests

| Story | Covered tasks | Independent verification |
|---|---|---|
| US1 — Book a service appointment | T009–T010, T016, T018, T020, T025–T028, T032–T034 | Submit a valid owned-vehicle booking and verify one Confirmed/Assigned appointment with exactly one technician and bay, UTC timestamps, Location, and no partial persistence. |
| US2 — Allocate eligible resources | T019–T020, T027, T032, T034 | Control qualification, compatibility, occupancy, and IDs; verify lowest-ID independent selection, fallback, and three approved resource errors. |
| US3 — Validate booking time | T016–T017, T022, T032, T034 | Verify offset parsing, whole-minute/future checks, operating hours, 17:00 boundary, inclusive horizon, February 29, DST, and elapsed duration. |
| US4 — Prevent conflicting bookings | T018, T023, T025, T027, T032–T034 | Use separate SQL Server connections to verify half-open conflicts, global vehicle checks, adjacency, serialization, rollback, and no double allocation. |
| US5 — Retry safely using idempotency | T021–T027, T028, T031, T033–T034 | Replay matching keys, reject mismatched keys, contend on same keys, reuse expired keys, and recover lost responses without duplicates. |
| US6 — Retrieve appointments and reference data | T029–T031, T033–T034 | Verify appointment 200/404 behavior and all three paginated reference collections with stable ordering and required metadata. |

## Implementation Strategy

### MVP scope

The MVP is US1 — Book a service appointment — after T005 authorization, all blocking foundation tasks,
and the minimum required transaction, time, resource, idempotency, API, and test tasks. US1 cannot be
considered complete without the approved conflict and idempotency guarantees that make a booking safe.

### Incremental delivery

1. Finish T001–T004 documentation and obtain authorization at T005.
2. Complete T006–T015 foundation and persistence verification.
3. Complete T016–T027 business, transaction, idempotency, and reliability work.
4. Deliver US1 through T028 and validate it independently.
5. Deliver US6 read endpoints through T029–T031 while maintaining the US1 checkpoint.
6. Complete T032–T035 quality verification, then T036–T037 delivery readiness.

### Notes

- Completed `[x]` tasks record previously verified documentation work; they do not authorize source
  implementation.
- T005 records the implementation authorization gate. The current T011-T015 authorization is
  separately recorded in the AI development log.
- T015 uses the explicitly authorized local SQL Server path and has completed real database verification.
- The current T016-T020 authorization is recorded in the AI development log; T021-T037 remain pending.
- No task creates Angular, Playwright, authentication/JWT, P1 lifecycle workflows, microservices,
  message queues, migrations without review, Docker configuration, or runtime infrastructure without
  separate authorization.
