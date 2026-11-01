# ADR-003: SQL Server application locks for booking serialization

**Status**: Accepted

## Context

Concurrent bookings can conflict on a vehicle, technician, or service bay. P0 requires consistent
revalidation and allocation without a database overlap trigger or exclusion constraint.

## Decision

Use SQL Server READ COMMITTED with `READ_COMMITTED_SNAPSHOT OFF` and transaction-owned exclusive
`sp_getapplock` locks. Acquire locks in the strict order Idempotency Key → Vehicle → Dealership, with
a five-second timeout per lock. Acquire the idempotency lock and inspect the existing unexpired record
first. Matching replay and mismatched conflict short-circuit after that first lock; new or expired
requests acquire Vehicle and then Dealership locks. Do not automatically retry booking transactions.

Direct database writes that bypass this protocol are a known integrity limitation.

## Rationale

The lock order prevents lock-order inversion, the resource locks serialize mutable booking state, and
the transaction boundary lets the application revalidate before atomic persistence.

## Consequences and tradeoffs

- Same-key requests can short-circuit without unnecessary resource locks.
- Dealership-level serialization reduces parallelism for bookings in one dealership.
- Five-second timeouts produce explicit 409/503 outcomes rather than indefinite waits.
- Writers that bypass the application protocol are not protected; no P0 overlap trigger is planned.

## Alternatives considered

- SQL overlap triggers or exclusion constraints: rejected because they are not planned for P0.
- READ_COMMITTED_SNAPSHOT: rejected because the approved isolation setting is READ COMMITTED with
  `READ_COMMITTED_SNAPSHOT OFF`.
- Automatic transaction retries: rejected by the constitution and approved reliability constraints.
