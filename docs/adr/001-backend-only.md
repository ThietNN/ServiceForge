# ADR-001: Backend-only P0 service

**Status**: Accepted

## Context

ServiceForge is an automotive service scheduling system whose approved P0 scope is a backend-only API.
The specification excludes authentication/JWT, an admin interface, Angular, Playwright, and unrelated
customer or dealership workflows.

## Decision

Build P0 as an ASP.NET Core Web API. Do not create an Angular frontend or Playwright test suite, and do
not add frontend, admin, or technician UI capabilities to P0.

## Rationale

The approved value is appointment booking, automatic resource allocation, reliable concurrency control,
idempotent retries, and read/reference endpoints. A backend-only boundary keeps that scope bounded and
reviewable.

## Consequences and tradeoffs

- API contracts, HTTP behavior, and integration tests are first-class P0 artifacts.
- Users require an external client or future approved UI.
- Authentication and authorization remain a known security limitation until explicitly approved.

## Alternatives considered

- Angular frontend: rejected because Angular is explicitly excluded from P0.
- Admin or technician UI: rejected because those workflows are explicitly excluded from P0.
- Full-stack delivery: rejected because it expands the approved backend-only scope.
