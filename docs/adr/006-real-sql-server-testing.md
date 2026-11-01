# ADR-006: Real SQL Server integration testing

**Status**: Accepted — amended 2026-11-02

## Context

Booking integrity depends on SQL Server isolation, application locks, deadlock behavior, transaction
atomicity, UTC functions, and concurrent connections. An in-memory substitute cannot prove those
behaviors.

The original decision selected SQL Server Testcontainers for repeatable integration tests. The user
subsequently authorized local SQL Server testing for this repository because Docker is unavailable.

## Decision

Use xUnit unit tests, `WebApplicationFactory` API integration tests, and real local SQL Server
integration tests through the `SERVICEFORGE_TEST_SQLSERVER_CONNECTION` environment variable. The
connection must target a dedicated ServiceForge test database and must not be committed or printed.
Concurrency tests use separate connections and controlled synchronization. EF Core InMemory and SQLite
are not accepted as proof of SQL Server behavior.

The original Testcontainers decision is superseded for the current local development environment. A
future approved environment may restore Testcontainers without changing the database contract.

## Rationale

The real SQL Server engine is required to verify READ COMMITTED behavior, application locks, rollback,
deadlocks, transaction boundaries, and idempotency. A dedicated local database preserves isolation
without requiring Docker in the current environment.

## Consequences and tradeoffs

- Local integration tests require a reachable SQL Server instance and an owner-approved dedicated test
  database.
- Connection configuration is environment-only; credentials must not enter source control or logs.
- Tests must control timing and connection boundaries carefully and avoid destructive parallel access.
- Unit tests remain useful for deterministic time, interval, allocation, and mapping rules.
- No test may be reported as passing unless it was actually executed.
- Testcontainers remains the historical fallback for environments where it is explicitly approved and
  available.

## Alternatives considered

- SQL Server Testcontainers: originally accepted, then superseded for this local environment because
  Docker is unavailable.
- EF Core InMemory or SQLite: rejected because neither proves SQL Server behavior.
- A permanent shared SQL Server database: rejected because integration tests require a dedicated,
  clearly named test database.
