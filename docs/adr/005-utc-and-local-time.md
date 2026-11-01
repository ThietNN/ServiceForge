# ADR-005: UTC storage with dealership-local validation

**Status**: Accepted

## Context

Customers submit explicit-offset start times, while operating hours and booking horizons belong to the
dealership's local timezone. Service duration must represent elapsed real time across daylight-saving
transitions.

## Decision

Normalize the represented request instant to UTC, calculate the end by adding elapsed service duration,
and persist and return appointment timestamps in UTC. Validate operating hours Monday–Friday 09:00–17:00
and the inclusive one-local-calendar-year horizon in the dealership timezone. Saturday and Sunday are
closed; no holiday calendar or minimum advance notice is added in P0. February 29 maps to February 28
when the following year lacks February 29. SQL Server UTC is authoritative for future-time and
idempotency-expiration decisions.

## Rationale

UTC persistence gives a single instant for conflict checks and response data, while local conversion
preserves dealership business meaning. Elapsed duration avoids wall-clock arithmetic errors during DST.

## Consequences and tradeoffs

- Clients must supply an explicit offset and receive UTC timestamps.
- Timezone data and DST rules are required for each dealership.
- A request offset need not equal the dealership's current offset; the represented instant remains
  authoritative.
- Local boundary testing must include DST and February 29 cases.

## Alternatives considered

- Persisting local timestamps: rejected because cross-dealership conflicts require comparable instants.
- Treating duration as wall-clock arithmetic: rejected because the requirement is elapsed real time.
- Using application local time: rejected because SQL Server UTC is the authoritative clock.
