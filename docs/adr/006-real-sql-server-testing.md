# ADR-006: Real SQL Server integration testing

**Status**: Accepted

## Context

Booking integrity depends on SQL Server isolation, application locks, deadlock behavior, transaction
atomicity, UTC functions, and concurrent connections. An in-memory substitute cannot prove those
behaviors.

## Decision

Use xUnit unit tests, `WebApplicationFactory` API integration tests, and real SQL Server integration
tests through Testcontainers. Concurrency tests use separate connections and controlled
synchronization. EF Core InMemory is not accepted as proof of SQL Server concurrency behavior.

## Rationale

The real database engine is required to verify READ COMMITTED behavior, `sp_getapplock`, rollback,
deadlocks, transaction boundaries, and idempotency under concurrency.

## Consequences and tradeoffs

- Tests require a SQL Server Testcontainers environment and are slower than pure unit tests.
- Tests must control timing and connection boundaries carefully.
- Unit tests remain useful for deterministic time, interval, allocation, and mapping rules.
- No test may be reported as passing unless it was actually executed.

## Alternatives considered

- EF Core InMemory: rejected because it does not prove SQL Server concurrency correctness.
- Mock-only persistence tests: rejected because mocks cannot validate SQL isolation or application
  locks.
- A permanent shared SQL Server: rejected for isolated, repeatable integration-test execution.
