# AI Development Log

## T001 — Documentation-only repository bootstrap

- Date: 2026-11-01
- AI assistance: OpenAI Codex
- Scope: Initialized the official GitHub Spec Kit Codex structure and created the requested documentation guidance and directories. No application implementation was performed.
- Verification performed: Inspected the repository before initialization; checked the installed Specify CLI version and initialization help; ran the official Codex initialization successfully; inspected the generated Spec Kit structure and confirmed the constitution remains the untouched official placeholder template; confirmed requested files/directories exist; checked Git status and confirmed this directory is not a Git repository.
- Unresolved questions: Approved specification, constitution, plan, data model, contracts, ADRs, and task text will be supplied separately.

## T002A — Approved ServiceForge Constitution v1.0

- Date: 2026-11-01
- AI assistance: OpenAI Codex using the repository's `speckit-constitution` workflow guidance.
- Scope: Replaced the official Spec Kit constitution placeholder with the approved eight-principle
  ServiceForge Constitution v1.0.0. No application implementation or other specification artifact
  was changed.
- Verification performed: Inspected `AGENTS.md` and the existing constitution; checked that no
  extension hooks were configured; resolved the active constitution template successfully; checked
  the completed constitution for unresolved bracketed template placeholders, version/date metadata,
  principle count, and the required approved topics; confirmed no application files, projects,
  migrations, Docker files, or other specification documents were modified.
- Unresolved questions: None for T002A.

## T002B — Approved ServiceForge P0 feature specification

- Date: 2026-11-01
- AI assistance: OpenAI Codex using the repository's `speckit-specify` workflow guidance.
- Scope: Created `specs/001-service-booking/spec.md` from the supplied approved P0 requirements,
  preserving the official Spec Kit structure. Added the required requirements-quality checklist.
  No application implementation or other design artifact was changed.
- Verification performed: Inspected `AGENTS.md`, the ratified constitution, the official specification
  and checklist templates, the existing feature directory, and Git status; confirmed no extension
  hooks were configured; verified the specification has six user stories, 96 uniquely identified
  functional requirements, 24 mandatory acceptance scenarios, required error mappings, exclusions,
  entities, assumptions, and measurable success criteria; checked for unresolved placeholders and
  contradictions against the constitution; ran `git diff --check`.
- Unresolved questions: None for the supplied approved requirements.

## T002B.1 — Reviewed specification wording correction

- Date: 2026-11-01
- AI assistance: OpenAI Codex using the repository's `speckit-specify` workflow guidance.
- Scope: Clarified that Spec Kit story priority `P1` means high-priority user story and is distinct
  from ServiceForge's deferred P1 implementation scope. Refined the concurrent-booking acceptance
  scenario so simultaneous requests are not implied to be universally mutually exclusive.
- Verification performed: Confirmed the specification retains six user stories, all 96 functional
  requirements, and all 24 mandatory acceptance scenarios; checked the corrected priority and
  concurrency wording; verified the requirements checklist remains included; ran `git diff --check`.
- Unresolved questions: None.

## T002C — Architecture plan and approved data model

- Date: 2026-11-01
- AI assistance: OpenAI Codex using the repository's `speckit-plan` workflow guidance.
- Scope: Created `specs/001-service-booking/plan.md` and `specs/001-service-booking/data-model.md`
  from the approved constitution, feature specification, checklist, and T002C instructions. The plan
  documents the four-project architecture, dependency direction, booking transaction workflow, API
  behavior, test strategy, sequencing, tradeoffs, and limitations. The data model documents exactly
  the ten approved tables without adding a DealershipServiceType relationship.
- Verification performed: Inspected all approved source documents and official Spec Kit templates;
  checked that no extension hooks were configured; resolved the official plan template successfully;
  verified the plan contains the approved architecture and lock workflow; verified the data model
  contains exactly ten tables, all supplied fields, keys, foreign keys, checks, indexes, nullable
  resource fields, no-cascade-delete rules, and no Processing state; checked P0/P1 boundaries and
  consistency with the constitution and feature specification; ran documentation checks and
  `git diff --check`.
- Unresolved questions: The approved requirements do not specify a target .NET version or numeric
  performance target; the plan records both as unspecified rather than inventing decisions.

## T002C.1 — Spec Kit resolution investigation and reviewed plan/model refinement

- Date: 2026-11-01
- AI assistance: OpenAI Codex using the repository's `speckit-plan` workflow guidance.
- Scope: Investigated the installed Spec Kit CLI and PowerShell path-resolution implementation;
  refined the booking workflow so matching idempotent replays avoid unnecessary vehicle/dealership
  locks; documented reviewed EF Core Code First migrations; selected `net10.0` from the inspected
  SDK/runtime and official support status; corrected Appointment-to-IdempotencyRecord cardinality to
  at most one; no application or runtime artifacts were created.
- Verification performed: Confirmed Specify CLI 1.2.5; inspected `setup-plan.ps1`, `common.ps1`, and
  `create-new-feature.ps1`; reproduced the missing-feature-context error and verified explicit
  `SPECIFY_FEATURE_DIRECTORY` resolution with `SPECIFY_FEATURE_NO_PERSIST=1` without creating
  `.specify/feature.json`; ran `dotnet --info` and `dotnet --list-sdks`; checked for `global.json`;
  reviewed plan/model consistency against the constitution and specification; ran `git diff --check`.
- Unresolved questions: Exact package versions and compatibility checks remain implementation-time
  work. No numeric performance target is specified.

## T002C.2 — Final architecture design verification

- Date: 2026-11-01
- AI assistance: OpenAI Codex.
- Scope: Recorded the verified Spec Kit feature-resolution root cause and supported invocation for
  shells without persisted feature context:
  `$env:SPECIFY_FEATURE_DIRECTORY = "specs/001-service-booking"` and
  `$env:SPECIFY_FEATURE_NO_PERSIST = "1"`.
- Resolution note: `SPECIFY_FEATURE_DIRECTORY` supplies the active feature directory when no
  persisted feature context is available. `SPECIFY_FEATURE_NO_PERSIST=1` is for read-only diagnostics
  and prevents the resolver from creating or updating `.specify/feature.json`; it is not required for
  normal persisted workflow use. No `.specify/feature.json` was created and no official Spec Kit
  script was modified.
- Verification performed: Confirmed branch `main`, configured `origin`, presence of the T002C plan,
  data model, and log; verified the approved architecture, ten-table schema, transaction and
  idempotency workflow, composite foreign keys, indexes, P0/P1 boundaries, and no unexpected files;
  ran documentation consistency checks and `git diff --check`.
- Unresolved questions: None for the approved architecture design checkpoint.

## T002D — Approved API contract and architecture decisions

- Date: 2026-11-02
- AI assistance: OpenAI Codex.
- Scope: Created `specs/001-service-booking/contracts/api.md` for the five approved P0 endpoints and
  seven Accepted ADRs under `docs/adr/`. The documents preserve the approved schema, transaction,
  idempotency, time, testing, architecture, and AI-governance decisions. No application code or
  migrations were created.
- Verification performed: Read the constitution, feature specification, plan, data model, checklist,
  and AI log; confirmed no existing approved ADR location; checked the contract for exactly five
  endpoints, approved error mappings, pagination, UTC response examples, idempotency behavior, and the
  P0 authentication limitation; checked all seven ADRs for required sections and consistency with the
  approved documents; ran documentation consistency checks and `git diff --check`.
- Unresolved questions: None for T002D.

## T002E — Approved implementation task breakdown

- Date: 2026-11-02
- AI assistance: OpenAI Codex using the repository's `speckit-tasks` workflow guidance.
- Scope: Created `specs/001-service-booking/tasks.md` with the approved T001–T037 structure. T001–T004
  are marked complete for prior documentation work; T005 remains an explicit unchecked implementation
  authorization checkpoint. T006–T037 remain pending implementation authorization.
- Verification performed: Read all authoritative specification, architecture, schema, contract, ADR,
  checklist, constitution, and repository guidance files; resolved the official task template with
  `SPECIFY_FEATURE_DIRECTORY=specs/001-service-booking` and
  `SPECIFY_FEATURE_NO_PERSIST=1`; confirmed no `.specify/feature.json` was created; verified task IDs,
  dependency ordering, P0 boundaries, user-story coverage, and acceptance-scenario coverage; ran
  documentation consistency checks and `git diff --check`.
- Unresolved questions: Implementation authorization remains pending and must be explicitly recorded
  before T006 or any source-code task begins.

## T005-T010 — Authorized implementation foundation batch

- Date: 2026-11-02
- AI assistance: OpenAI Codex using the repository's `speckit-implement` workflow guidance.
- Authorization: The user explicitly authorized P0 implementation on 2026-11-02 for T005-T010 only.
- Scope: Recorded T005 authorization; created the .NET 10 solution and four Clean Architecture
  production projects; configured inward project references and DI registration extension points;
  added approved foundation packages; implemented framework-independent Domain entities and enums;
  added Application CQRS contracts, DTOs, focused abstractions, and request-level validators; and
  added focused xUnit tests for the pure Domain and Application behavior. No DbContext, migrations,
  database, SQL locks, booking transaction, API endpoints, Docker configuration, or P1 workflow was
  created.
- Package versions: `MediatR` 14.2.0, `FluentValidation` 12.2.0,
  `FluentValidation.DependencyInjectionExtensions` 12.2.0, `Microsoft.EntityFrameworkCore.SqlServer`
  10.0.14, `Microsoft.AspNetCore.OpenApi` 10.0.14, and xUnit template dependencies including
  `coverlet.collector` 6.0.4.
- Verification performed: Ran `dotnet restore ServiceForge.slnx`; ran
  `dotnet build ServiceForge.slnx --no-restore` successfully with four projects and zero errors;
  ran the focused unit test project successfully; checked project references and target frameworks;
  verified no API route or persistence implementation was added; ran `git diff --check`.
- Warning: Restore/build reported NU1902 and NU1903 advisories for transitive
  `System.Security.Cryptography.Pkcs` 9.0.11. No dependency upgrade was applied in this bounded
  batch; the warning remains for review before release.
- Unresolved questions: WebApplicationFactory, Testcontainers, EF Core DbContext, migrations, and
  booking transaction implementation remain scheduled for later tasks and were not started.

## T011-T015 — SQL Server persistence foundation batch

- Date: 2026-11-02
- AI assistance: OpenAI Codex using the repository's `speckit-implement` workflow guidance.
- Authorization: The user explicitly authorized P0 implementation of T011-T015 on 2026-11-02.
- Scope: Added the EF Core SQL Server DbContext and Fluent configurations for exactly the approved ten
  tables; added composite ownership/resource foreign keys, checks, indexes, non-cascading deletes,
  the filtered unique AppointmentId idempotency index, and deterministic reference-data seeding;
  generated and reviewed the `InitialCreate` Code First migration; added environment-driven design-time
  migration support; and created a separate Testcontainers SQL Server integration-test project.
  No T016+ booking rules, transaction locks, API endpoints, Docker files, or runtime infrastructure
  were added.
- Package versions: upgraded direct `Microsoft.Data.SqlClient` to 6.1.8; retained
  `Microsoft.EntityFrameworkCore.SqlServer` 10.0.14; added `Microsoft.EntityFrameworkCore.Design`
  10.0.14; added `Testcontainers.MsSql` 4.16.0. EF CLI was updated to 10.0.14.
- Advisory remediation: `Microsoft.EntityFrameworkCore.SqlServer` 10.0.14 resolved
  `Microsoft.Data.SqlClient` 6.1.7 and `System.Security.Cryptography.Pkcs` 9.0.11. The explicit
  compatible `Microsoft.Data.SqlClient` 6.1.8 reference now resolves `System.Security.Cryptography.Pkcs`
  9.0.21. `dotnet list ServiceForge.slnx package --include-transitive --vulnerable` reports no
  vulnerable packages.
- Verification performed: Ran `dotnet list ServiceForge.slnx package --include-transitive --vulnerable`,
  `dotnet nuget why ServiceForge.slnx System.Security.Cryptography.Pkcs`, `dotnet restore ServiceForge.slnx`,
  `dotnet build ServiceForge.slnx --no-restore` with zero errors and warnings, six existing unit tests
  with all six passing, migration static checks confirming ten tables, required names, varchar statuses,
  no cascade actions, no forbidden tables/states, and the filtered unique idempotency index, plus
  `git diff --check`.
- Blocker: `docker version` could not run because Docker is not installed. T015 integration tests were
  not executed and T015 remains unchecked; no SQL Server/Testcontainers result is claimed.
- Unresolved questions: The real-container migration, schema, seed, READ_COMMITTED_SNAPSHOT, and
  constraint tests require Docker availability and remain pending T015 verification.

## T015 — Local SQL Server integration completion

- Date: 2026-11-02
- AI assistance: OpenAI Codex using the repository's `speckit-implement` workflow guidance.
- Authorization: The user explicitly authorized replacing the unavailable Testcontainers execution path
  with the local SQL Server instance for T015.
- Environment discovery: A local SQL Server connection using Windows Authentication succeeded, and the
  dedicated `ServiceForge_IntegrationTests` database was created/accessed. No credentials or
  credential-bearing connection strings were recorded.
- Scope: Removed the Testcontainers package and Docker-specific fixture usage. The integration fixture
  now reads `SERVICEFORGE_TEST_SQLSERVER_CONNECTION`, applies the initial migration, and validates the
  ten application tables, seed data, non-cascading foreign keys, unique idempotency index, and
  `READ_COMMITTED_SNAPSHOT OFF`. ADR-006 and the plan/tasks now document local SQL Server as the
  current authorized path while preserving Testcontainers as historical context.
- Verification performed: The first real run found the expected EF metadata table and corrected the
  assertion to exclude `__EFMigrationsHistory`; the rerun passed both integration tests. Ran
  `dotnet restore ServiceForge.slnx`, `dotnet build ServiceForge.slnx --no-restore` with zero warnings
  and errors, six unit tests with all six passing, two SQL Server integration tests with both passing,
  the NuGet vulnerability audit with no vulnerable packages, `dotnet nuget why` confirming patched
  SqlClient resolution, and `git diff --check`.
- Unresolved questions: None for T015. Later concurrency and booking behavior remain scheduled for
  T016 and later tasks.

## T016-T020 — Booking business rules and resource selection

- Date: 2026-11-02
- AI assistance: OpenAI Codex using the repository's `speckit-implement` workflow guidance.
- Authorization: The user explicitly authorized P0 implementation of T016-T020 on 2026-11-02.
- Scope: Added UTC booking-time normalization using the existing authoritative-clock abstraction;
  dealership-local weekday operating hours, inclusive calendar-year horizon, leap-day handling, and
  instant-based timezone conversion; vehicle ownership and half-open occupied-overlap rules; EF Core
  queries for ownership, cross-dealership vehicle conflicts, technician qualification, bay compatibility,
  active resources, and full-interval availability; and deterministic independent lowest-ID technician
  and bay allocation. No T021+ transaction, idempotency, SQL-lock, HTTP, or P1 behavior was added.
- Verification performed: Added focused unit coverage for explicit offsets, whole-minute precision,
  UTC conversion, strict future validation, half-open adjacency, operating-hour/weekend boundaries,
  inclusive horizon and leap-day behavior, ownership, cancellation occupancy, error distinctions, and
  deterministic allocation. Added local SQL Server integration coverage for ownership, cross-dealership
  conflicts, adjacent intervals, independent vehicles, qualifications, compatibility, inactive and
  occupied resources, fallback selection, and approved unavailable-resource errors. Ran the unit suite
  with 19 passing tests and the SQL Server integration suite with 6 passing tests; build completed with
  zero warnings and errors.
- Unresolved questions: T021 and later transaction/idempotency/API work remains pending.

## T021-T025 - SQL locking and idempotency infrastructure

- Date: 2026-11-02
- AI assistance: OpenAI Codex using the repository's `speckit-implement` workflow guidance.
- Authorization: The user explicitly authorized P0 implementation of T021-T025 on 2026-11-02.
- Scope: Added typed canonical SHA-256 request identity hashing, a SQL Server `SYSUTCDATETIME()` clock,
  transaction-owned exclusive `sp_getapplock` support with deterministic parameterized resources and
  five-second timeouts, idempotency lookup/replay/conflict/expiry behavior, and atomic persistence of
  a fully assigned Confirmed appointment with its successful idempotency response. No booking
  orchestration, HTTP endpoints, concurrency scenario suite, Processing state, or P1 behavior was added.
- Verification performed: The solution build passed with zero warnings and errors; the unit suite passed
  21 tests; the local SQL Server integration suite passed 9 tests covering the SQL clock, lock timeout
  and release, idempotency matching/conflict/expiry replacement, atomic commit, and rollback. The
  implementation uses the existing dedicated integration-test connection supplied at test runtime;
  no credentials or connection values were recorded. NuGet vulnerability audit and `git diff --check`
  were run for this checkpoint.
- Unresolved questions: Booking command orchestration, API contracts, controlled multi-request booking
  concurrency tests, and later tasks remain pending T026 and beyond.

## T026-T027 - Atomic booking orchestration and concurrency verification

- Date: 2026-11-02
- AI assistance: OpenAI Codex using the repository's `speckit-implement` workflow guidance.
- Authorization: The user explicitly authorized P0 implementation of T026-T027 on 2026-11-02.
- Scope: Added the Application CreateAppointment command handler with typed outcomes and the approved
  transaction sequence: request validation, canonical identity, SQL Server UTC, idempotency lock and
  immediate lookup, ordered vehicle/dealership locks, locked revalidation, independent lowest-ID
  allocation, atomic persistence, commit, replay, conflict, and transient failure classification.
  Added controlled local SQL Server integration tests using separate service scopes/connections for
  same-key contention, same-key conflict, vehicle conflict, resource allocation, replay lock avoidance,
  expired-key replacement, rollback, and lock timeout mappings. No HTTP endpoints or T028+ work was added.
- Verification performed: `dotnet build ServiceForge.slnx --no-restore` passed with zero warnings/errors;
  the SQL Server integration suite passed 18 tests on the final run after adding cross-dealership vehicle
  serialization coverage; the prior 17-test suite also passed on two consecutive repeat runs;
  the initial unit suite remains 21 passing tests. Tests assert persisted state and clean up generated
  records. No credentials, connection values, or private environment investigation details were recorded.
- Known limitation: The SQL deadlock-victim classification path maps to `BOOKING_TEMPORARILY_UNAVAILABLE`
  without automatic retry, but no artificial deadlock was induced because doing so would be nondeterministic.
- Unresolved questions: HTTP endpoint exposure and API contract tests remain pending T028-T031; broader
  acceptance and regression completion remain pending T032-T037.

## T028-T031 - P0 HTTP API

- Date: 2026-11-02
- AI assistance: OpenAI Codex using the repository's `speckit-implement` workflow guidance.
- Authorization: The user explicitly authorized P0 implementation of T028-T031 on 2026-11-02.
- Scope: Added exactly the five approved HTTP routes: appointment creation, appointment retrieval, and
  the three paginated reference-data collections. Added SQL-backed read handlers, API composition and
  DbContext registration, JSON enum formatting, RFC 9457-compatible Problem Details with machine-readable
  codes and validation details, Retry-After mapping for transient outcomes, and development OpenAPI.
  No authentication, JWT, frontend, background worker, extra route, or P1 workflow was added.
- Verification performed: Added a WebApplicationFactory test project using the existing dedicated local
  SQL Server configuration. Four HTTP integration tests passed, covering successful POST/GET, Location,
  replay and conflict, missing key, invalid timestamp, pagination defaults/bounds, all three collections,
  missing appointment, ownership failure, and vehicle conflict. The full solution build passed with zero
  warnings/errors; the existing 21 unit tests and 18 SQL Server infrastructure/concurrency tests passed.
  NuGet vulnerability audit reported no vulnerable packages, and `git diff --check` passed. No credentials,
  connection values, or private environment investigation details were recorded.
- Unresolved questions: Final acceptance/regression and delivery documentation remain pending T032-T037.

## T032-T035 - P0 acceptance audit and regression verification

- Date: 2026-11-02
- AI assistance: OpenAI Codex using the repository's `speckit-implement` workflow guidance.
- Authorization: The user explicitly authorized P0 verification and regression work for T032-T035 on
  2026-11-02.
- Scope: Added the 24-scenario acceptance traceability matrix at `docs/acceptance-traceability.md`,
  added deterministic DST/elapsed-duration and equivalent-offset identity tests, expanded HTTP assertions,
  and corrected Retry-After mapping to the two approved transient error codes. No T036-T037 delivery work,
  P1 functionality, authentication, frontend, or additional endpoint was added.
- Verification performed: `dotnet restore ServiceForge.slnx` passed; the solution build passed with zero
  warnings/errors; 23 unit tests, 6 WebApplicationFactory HTTP tests, and 18 SQL Server integration tests
  passed. The controlled concurrency class passed 9 tests in each of three consecutive runs. A named
  semaphore serializes the two SQL-backed integration test hosts against the shared test database; the
  prior cross-host lock race was eliminated without changing production semantics. The NuGet vulnerability
  audit reported no vulnerable packages and `git diff --check` passed. No credentials, connection values,
  or private environment investigation details were recorded.
- Coverage status: All 24 mandatory scenarios are covered in the matrix. The lost-response scenario uses
  a deterministic post-commit response-discard simulation and verifies the persisted appointment and
  current idempotency record before retry. The exhaustive HTTP mapping test exercises every approved
  public code/status mapping and both Retry-After cases. SQL deadlock classification remains implemented
  without an artificial deadlock claim.
- Unresolved questions: T036-T037 delivery documentation and final submission readiness remain pending.

## T036-T037 - Delivery readiness and final submission review

- Date: 2026-11-02
- AI assistance: OpenAI Codex using the repository's `speckit-implement` workflow guidance.
- Authorization: The user explicitly authorized the T036-T037 delivery-readiness batch on 2026-11-02.
- Scope: Added reviewer-facing setup and architecture documentation in `README.md`; documented the
  four-project structure, actual configuration keys, manual EF migration command, migration-backed
  deterministic `HasData` seeding, API examples, test commands, Spec Kit feature context, platform
  qualifications, approved limitations, and P0 exclusions. No source behavior, migration, Docker
  configuration, runtime infrastructure, or approved specification was changed.
- Verification performed: Inspected startup and design-time configuration to confirm there is no
  automatic migration or startup seed operation. Confirmed the reviewed migration is
  `20261101173028_InitialCreate` and that reference data is applied through EF `HasData`. Confirmed
  .NET SDK 10.0.403 and `dotnet-ef` 10.0.14 are available. With the configured dedicated test
  connection, the schema/connectivity tests passed 2/2, all SQL Server integration tests passed
  18/18, all WebApplicationFactory HTTP integration tests passed 6/6, and the concurrency class
  passed 9/9 in each of three consecutive runs. Final restore, build, and unit verification also
  passed; the vulnerability audit reported no vulnerable packages and `git diff --check` passed.
  Database migration, seed data, READ_COMMITTED_SNAPSHOT, cleanup, and isolation checks therefore
  completed through the repository's actual tests. No connection value or credential was recorded.
- Cross-platform status: The .NET solution and Windows PowerShell workflow are verified in the
  repository environment. Complete macOS/Linux execution and cross-platform Spec Kit PowerShell
  invocation remain unverified. SQL Server authentication and time-zone identifier behavior may
  require OS-specific setup; README documents this without claiming compatibility.
- Unresolved questions: The retrospective AI decision/assumption audit requested for a later stage
  has not been started. Direct database writers that bypass the booking lock protocol and artificial
  SQL deadlock induction remain documented limitations.

## Batch R1 - Post-implementation audit corrections

- Date: 2026-11-02
- AI assistance: OpenAI Codex using the repository's `speckit-implement` workflow guidance.
- Authorization: The owner explicitly authorized corrections for H-001, H-002, C-001, and M-002;
  no other changes were authorized.
- Changes: New bookings now obtain fresh SQL Server UTC after vehicle and dealership locks before
  mutable time validation and persistence. Timestamp parsing now uses strict invariant ISO 8601
  formats with `Z` or an explicit offset in both request validation and booking normalization.
  Added deterministic regression coverage for post-lock future-time revalidation, valid/invalid
  timestamp formats, and the Development OpenAPI document's five approved operations. Reconciled
  the constitution and FR-089 with the previously approved dedicated local SQL Server path; real
  SQL Server remains mandatory, while Testcontainers is optional when explicitly approved and
  available. No new task IDs, endpoints, infrastructure, or P1 behavior were added.
- Verification: Restore passed. The final standalone build passed with zero warnings and errors. The
  complete unit suite passed 31 tests, the HTTP integration suite passed 7 tests, and the SQL Server
  integration suite passed 19 tests. The concurrency class passed 10 tests in each of three
  consecutive runs, including the delayed-lock regression. The focused OpenAPI test and strict time
  parsing tests passed. The NuGet vulnerability audit reported no vulnerable packages and
  `git diff --check` passed. All 24 acceptance scenarios remain covered; no SQL deadlock was induced.
- Remaining limitation: SQL deadlock classification remains implemented but has not been verified by
  inducing an artificial SQL deadlock.
