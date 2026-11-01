# ADR-004: Atomic idempotency and appointment persistence

**Status**: Accepted

## Context

Clients may retry after timeouts or lost responses. The system must prevent duplicate successful
bookings and must preserve enough response data to replay a successful result.

## Decision

Require a globally scoped `Idempotency-Key` on booking creation. Compute a typed canonical identity from
customerId, vehicleId, dealershipId, serviceTypeId, and normalized UTC start instant, then store its
SHA-256 hash. Retain successful responses for 24 hours. Persist the fully assigned Confirmed appointment
and successful idempotency response atomically in one transaction. Do not commit a separate Processing
state. Expired-key replacement is atomic and preserves the original appointment; if replacement fails,
the previous record and appointment remain. Clients retry uncertain outcomes with the original key.

## Rationale

The stored response makes retries deterministic, while atomic persistence prevents a successful response
from referring to an appointment that was not committed.

## Consequences and tradeoffs

- Matching retries can replay the exact stored 201 response and Location.
- A mismatched unexpired key returns `IDEMPOTENCY_KEY_CONFLICT`.
- Response bodies are retained for 24 hours and require explicit expiration handling.
- An uncertain commit cannot be treated as rollback; retry behavior is part of the contract.

## Alternatives considered

- Client-only deduplication: rejected because it cannot protect the server against concurrent retries.
- Separately committed Processing records: rejected because P0 requires no separately committed
  Processing state.
- Persisting only a request hash: rejected because successful response replay requires stored response
  status and body.
