# ServiceForge P0 API Contract

**Feature**: [001-service-booking](../spec.md)

**Status**: Approved P0 contract

This contract defines the five approved P0 endpoints. It does not add authentication, admin, Angular,
Playwright, cancellation, rescheduling, or other P1 workflows.

## Common Conventions

- JSON property names use camelCase.
- Appointment timestamps are returned as ISO 8601 UTC strings with a `Z` suffix.
- Booking creation requires the `Idempotency-Key` request header.
- Errors use RFC 9457 Problem Details with a machine-readable `code` extension.
- Validation errors may include field-level details where applicable.
- Collection endpoints use database-level offset pagination with stable ascending ID ordering.

## Endpoint Summary

| Method | Path |
|---|---|
| POST | `/api/appointments` |
| GET | `/api/appointments/{id}` |
| GET | `/api/customers/{id}/vehicles` |
| GET | `/api/dealerships` |
| GET | `/api/service-types` |

## 1. Create Appointment

### Request

```http
POST /api/appointments HTTP/1.1
Content-Type: application/json
Idempotency-Key: booking-20261103-0001
```

```json
{
  "customerId": 1,
  "vehicleId": 1,
  "dealershipId": 1,
  "serviceTypeId": 1,
  "startTime": "2026-11-03T10:00:00+07:00"
}
```

`startTime` MUST be ISO 8601 with an explicit UTC offset. The represented instant is authoritative;
the dealership timezone is used for local operating-hours and horizon validation.

### Successful response

```http
HTTP/1.1 201 Created
Location: /api/appointments/1001
Content-Type: application/json
```

```json
{
  "id": 1001,
  "status": "Confirmed",
  "assignmentStatus": "Assigned",
  "customerId": 1,
  "vehicleId": 1,
  "dealershipId": 1,
  "serviceTypeId": 1,
  "technicianId": 10,
  "serviceBayId": 20,
  "scheduledStartUtc": "2026-11-03T03:00:00Z",
  "scheduledEndUtc": "2026-11-03T04:00:00Z",
  "durationMinutes": 60
}
```

The example assumes a 60-minute seeded service type. The actual duration is read from
`ServiceType.DurationMinutes`, represents elapsed real time, and is snapshotted into the appointment.

The response is replayable for the same unexpired idempotency key and canonical request. A successful
replay returns the stored successful 201 response and the stored `Location` value.

### Idempotency behavior

- `Idempotency-Key` is required and globally scoped.
- Canonical request identity uses typed `customerId`, `vehicleId`, `dealershipId`, `serviceTypeId`,
  and normalized UTC start instant.
- JSON property order does not affect identity.
- Equivalent offset representations of the same instant produce the same identity.
- A matching unexpired key replays the stored successful 201 response and Location without acquiring
  unnecessary vehicle/dealership locks.
- A mismatched unexpired key returns 409 `IDEMPOTENCY_KEY_CONFLICT`.
- A key whose successful record expired after 24 hours may be reused atomically for a new booking.
- Expired-key replacement never deletes or modifies the original appointment. If replacement fails,
  the transaction rollback preserves the previous record and appointment.
- A failed booking does not persist a successful idempotency result.
- An uncertain commit outcome is not assumed to be rollback; the client retries with the original key.

## 2. Get Appointment

### Request

```http
GET /api/appointments/1001 HTTP/1.1
Accept: application/json
```

### Successful response

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "id": 1001,
  "status": "Confirmed",
  "assignmentStatus": "Assigned",
  "customerId": 1,
  "vehicleId": 1,
  "dealershipId": 1,
  "serviceTypeId": 1,
  "technicianId": 10,
  "serviceBayId": 20,
  "scheduledStartUtc": "2026-11-03T03:00:00Z",
  "scheduledEndUtc": "2026-11-03T04:00:00Z",
  "durationMinutes": 60
}
```

An absent appointment returns 404 with `RESOURCE_NOT_FOUND`.

## 3. List Customer Vehicles

### Request

```http
GET /api/customers/1/vehicles?page=1&pageSize=20 HTTP/1.1
Accept: application/json
```

Query parameters:

- `page`: 1-based; default `1`.
- `pageSize`: default `20`; maximum `100`.

Results are ordered by ID ascending and pagination is applied in the database.

### Successful response

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "items": [
    {
      "id": 1,
      "customerId": 1,
      "registrationNumber": "ABC-123"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

## 4. List Dealerships

### Request

```http
GET /api/dealerships?page=1&pageSize=20 HTTP/1.1
Accept: application/json
```

Query parameters and ordering follow the common collection conventions.

### Successful response

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "items": [
    {
      "id": 1,
      "name": "Central Service Centre",
      "timeZoneId": "Asia/Bangkok"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

## 5. List Service Types

### Request

```http
GET /api/service-types?page=1&pageSize=20 HTTP/1.1
Accept: application/json
```

Query parameters and ordering follow the common collection conventions.

### Successful response

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "items": [
    {
      "id": 1,
      "name": "Standard Service",
      "durationMinutes": 60
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

## Problem Details

All business and validation errors use RFC 9457 Problem Details. The `code` member is machine-readable;
`errors` may provide field-level validation details.

Example:

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
```

```json
{
  "type": "https://example.invalid/problems/invalid-booking-time",
  "title": "Invalid booking time",
  "status": 400,
  "detail": "startTime must include an explicit UTC offset and be aligned to a whole minute.",
  "code": "INVALID_BOOKING_TIME",
  "errors": {
    "startTime": [
      "The value must include an explicit UTC offset.",
      "The value must have zero seconds and zero fractional seconds."
    ]
  }
}
```

Approved mappings:

| HTTP status | Codes |
|---|---|
| 400 | `INVALID_REQUEST`, `INVALID_BOOKING_TIME`, `OUTSIDE_OPERATING_HOURS`, `BOOKING_HORIZON_EXCEEDED` |
| 403 | `VEHICLE_NOT_OWNED` |
| 404 | `RESOURCE_NOT_FOUND` |
| 409 | `VEHICLE_TIME_CONFLICT`, `NO_QUALIFIED_TECHNICIAN`, `NO_COMPATIBLE_BAY`, `NO_AVAILABLE_RESOURCES`, `IDEMPOTENCY_KEY_CONFLICT`, `REQUEST_IN_PROGRESS` |
| 503 | `BOOKING_TEMPORARILY_UNAVAILABLE` |

Example idempotency conflict:

```json
{
  "type": "https://example.invalid/problems/idempotency-key-conflict",
  "title": "Idempotency key conflict",
  "status": 409,
  "detail": "The supplied Idempotency-Key was already used for a different request.",
  "code": "IDEMPOTENCY_KEY_CONFLICT"
}
```

For `REQUEST_IN_PROGRESS` and `BOOKING_TEMPORARILY_UNAVAILABLE`, the response includes:

```http
Retry-After: 2
```

`REQUEST_IN_PROGRESS` applies when the idempotency lock cannot be acquired within five seconds.
`BOOKING_TEMPORARILY_UNAVAILABLE` applies to vehicle/dealership lock timeout or SQL deadlock.

## P0 Security Boundary

Authentication and JWT are excluded from P0. The supplied `customerId` is request data and is not an
authenticated identity. Vehicle ownership validation therefore validates the supplied customer context
against the vehicle owner but does not provide production authorization. This API is not production-
secure without authorization controls; adding those controls requires explicit approval and is outside
this P0 contract.
