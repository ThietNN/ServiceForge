<!--
Sync Impact Report
- Version change: placeholder → 1.0.0
- Modified principles: replaced the five placeholder principles with eight approved
  ServiceForge principles I–VIII.
- Added sections: Scope and Deferred Work; Compliance and Change Control.
- Removed sections: placeholder examples and unresolved template fields.
- Follow-up TODOs: none.
-->

# ServiceForge Constitution

## Core Principles

### I. Specification-Driven Development

- Approved specifications are the source of truth.
- Implementation tasks MUST trace to approved requirements and acceptance criteria.
- AI agents MUST NOT invent requirements, introduce features, or silently change architecture.
- Ambiguities and conflicts MUST be reported before implementation.
- Changes to approved decisions require explicit approval.

### II. Architecture and Technology

- The approved technology stack is ASP.NET Core, C#, SQL Server, and EF Core.
- The system MUST use Clean Architecture with API, Application, Domain, and Infrastructure
  layers.
- The system MUST use CQRS with MediatR.
- The system MUST use built-in dependency injection and focused repository interfaces where
  justified.
- The EF Core DbContext acts as the Unit of Work.
- The Domain layer MUST NOT depend on Infrastructure or Presentation.
- Microservices, message queues, and additional frameworks require explicit approval.

### III. Business Integrity

- Only approved P0 requirements MUST be implemented.
- New bookings require one active qualified technician and one active compatible bay.
- Overlapping vehicle, technician, and bay appointments MUST be prevented.
- Vehicle ownership and dealership resource boundaries MUST be enforced.
- Dealership-local operating hours MUST be validated, and timestamps MUST be persisted in UTC.
- Service duration represents elapsed real time.
- There MUST be no direct Dealership–ServiceType catalog relationship.
- P1 workflows remain deferred.

### IV. Transaction and Concurrency

- SQL Server MUST use READ COMMITTED isolation with READ_COMMITTED_SNAPSHOT OFF.
- Transaction-owned exclusive `sp_getapplock` locks MUST be used.
- The global lock order is: Idempotency Key → Vehicle → Dealership.
- Each lock acquisition has a five-second timeout.
- Booking constraints MUST be revalidated after acquiring locks.
- The appointment and successful idempotency record MUST be committed atomically.
- An uncertain commit outcome MUST NOT be assumed to be a rollback.
- Automatic server-side transaction retries are prohibited.

### V. API and Data

- APIs MUST follow approved REST contracts.
- Booking start times MUST include an explicit ISO 8601 UTC offset.
- Appointment timestamps returned by the API MUST be in UTC.
- API errors MUST use RFC 9457 Problem Details with machine-readable business error codes.
- Booking creation MUST require `Idempotency-Key`.
- Collection endpoints MUST use offset pagination.
- Authentication, an admin interface, Angular, and Playwright are excluded from P0.

### VI. Quality

- Unit and integration tests are mandatory.
- Database integration tests MUST use a real SQL Server through Testcontainers.
- Tests MUST verify concurrency, idempotency, ownership, timezone behavior, rollback, and error
  contracts.
- EF Core InMemory is not proof of SQL Server concurrency correctness.
- Tests MUST never be claimed as passed unless they were actually executed successfully.
- Known limitations MUST be documented.

### VII. AI-Assisted Engineering

- GitHub Spec Kit MUST be used for specifications, plans, and tasks.
- Codex or Cursor MAY be used only for bounded, approved tasks.
- AI-generated changes MUST be reviewed for correctness, architecture, security, and testing.
- Meaningful AI contributions, corrections, and verification MUST be recorded.
- Requirements MUST remain traceable to implementation and tests.

### VIII. Authorization and Governance

- Specification approval does not authorize application code.
- Application code, migrations, and runtime infrastructure require explicit authorization.
- Conflicts MUST be reported before making deviations.
- Amendments MUST be explicit and documented.

## Scope and Deferred Work

The constitution governs the ServiceForge backend-only automotive service scheduling system.
The P0 scope is limited to approved requirements. P1 workflows and any capability not covered by
an approved specification remain deferred until explicitly approved.

## Compliance and Change Control

Every implementation plan, task list, code change, and test change MUST be reviewable against the
approved specifications and this constitution. Any conflict, ambiguity, or requested deviation
MUST be documented and explicitly approved before implementation proceeds.

## Governance

This constitution is authoritative for project governance and supplements, but does not replace,
approved feature specifications. Amendments require explicit approval, a documented rationale,
an updated version and amendment date, and review of affected specifications, plans, tasks, code,
and tests. The semantic version MUST be incremented according to the significance of the
governance change. Compliance MUST be reviewed during specification, planning, implementation,
and code review. No agent may treat approval of a specification as authorization for work outside
the approved task.

**Version**: 1.0.0 | **Ratified**: 2026-11-01 | **Last Amended**: 2026-11-01
