# ServiceForge Service Booking Data Model

**Feature**: [001-service-booking](./spec.md)

**Status**: Approved P0 design, pending implementation authorization

This model defines exactly ten tables for the ServiceForge P0 booking feature. It uses SQL Server
`bigint IDENTITY` primary keys where applicable, `datetime2(7)` UTC timestamps, positive integer
service durations, `bit` active flags, `varchar(32)` status fields, `nvarchar` names, and no cascade
deletes. P0 creates only fully assigned `Confirmed` appointments; nullable appointment resource IDs
and selected lifecycle values anticipate approved P1 schema needs.

No `DealershipServiceType` table or direct Dealership–ServiceType relationship exists.

## Table Inventory

| # | Table | Purpose |
|---:|---|---|
| 1 | `Customer` | Customer identity and creation time. |
| 2 | `Vehicle` | Customer-owned vehicle identity. |
| 3 | `Dealership` | Dealership identity and local timezone. |
| 4 | `ServiceType` | Globally defined service and positive elapsed duration. |
| 5 | `Technician` | Dealership-owned active technician resource. |
| 6 | `ServiceBay` | Dealership-owned active service bay resource. |
| 7 | `TechnicianQualification` | Many-to-many technician/service qualification. |
| 8 | `BayServiceCompatibility` | Many-to-many bay/service compatibility. |
| 9 | `Appointment` | Booking, assigned resources, UTC interval, and lifecycle state. |
| 10 | `IdempotencyRecord` | Global successful booking response and canonical request hash. |

## Common Conventions

- Primary key identifiers are `bigint IDENTITY` for entity tables where an identity key is applicable.
- All timestamps use `datetime2(7)` and are stored in UTC.
- Names use `nvarchar` with the approved lengths below.
- Active flags use `bit` and are `NOT NULL`.
- Status fields use `varchar(32)` and are constrained to approved values.
- Foreign keys use `NO ACTION`/non-cascading delete behavior. No cascade deletes are permitted.
- SQL Server UTC is authoritative for future-time validation and idempotency-expiration decisions.

## 1. Customer

| Column | Type | Nullability | Key/constraint | Meaning |
|---|---|---|---|---|
| `Id` | `bigint IDENTITY` | NOT NULL | Primary key | Customer identifier. |
| `Name` | `nvarchar(200)` | NOT NULL | — | Customer name. |
| `CreatedAtUtc` | `datetime2(7)` | NOT NULL | — | Creation timestamp in UTC. |

## 2. Vehicle

| Column | Type | Nullability | Key/constraint | Meaning |
|---|---|---|---|---|
| `Id` | `bigint IDENTITY` | NOT NULL | Primary key; participates in `UNIQUE (Id, CustomerId)` | Vehicle identifier. |
| `CustomerId` | `bigint` | NOT NULL | Foreign key to `Customer`; participates in `UNIQUE (Id, CustomerId)` | Sole owner. |
| `RegistrationNumber` | `nvarchar(32)` | NOT NULL | — | Vehicle registration. |
| `CreatedAtUtc` | `datetime2(7)` | NOT NULL | — | Creation timestamp in UTC. |

Indexes and constraints:

- `UNIQUE (Id, CustomerId)` supports the Appointment composite ownership foreign key.
- `INDEX (CustomerId, Id)` supports customer vehicle retrieval and ownership checks.
- A vehicle belongs to exactly one customer.

## 3. Dealership

| Column | Type | Nullability | Key/constraint | Meaning |
|---|---|---|---|---|
| `Id` | `bigint IDENTITY` | NOT NULL | Primary key | Dealership identifier. |
| `Name` | `nvarchar(200)` | NOT NULL | — | Dealership name. |
| `TimeZoneId` | `nvarchar(100)` | NOT NULL | — | Timezone used for local operating-hour and horizon validation. |
| `CreatedAtUtc` | `datetime2(7)` | NOT NULL | — | Creation timestamp in UTC. |

There is no foreign key from Dealership to ServiceType and no DealershipServiceType table.

## 4. ServiceType

| Column | Type | Nullability | Key/constraint | Meaning |
|---|---|---|---|---|
| `Id` | `bigint IDENTITY` | NOT NULL | Primary key | Service type identifier. |
| `Name` | `nvarchar(200)` | NOT NULL | — | Service type name. |
| `DurationMinutes` | `int` | NOT NULL | `CHECK (DurationMinutes > 0)` | Global elapsed service duration. |
| `CreatedAtUtc` | `datetime2(7)` | NOT NULL | — | Creation timestamp in UTC. |

## 5. Technician

| Column | Type | Nullability | Key/constraint | Meaning |
|---|---|---|---|---|
| `Id` | `bigint IDENTITY` | NOT NULL | Primary key; participates in `UNIQUE (Id, DealershipId)` | Technician identifier. |
| `DealershipId` | `bigint` | NOT NULL | Foreign key to `Dealership`; participates in `UNIQUE (Id, DealershipId)` | Sole dealership ownership. |
| `Name` | `nvarchar(200)` | NOT NULL | — | Technician name. |
| `IsActive` | `bit` | NOT NULL | — | Eligibility flag. |
| `CreatedAtUtc` | `datetime2(7)` | NOT NULL | — | Creation timestamp in UTC. |

Indexes and constraints:

- `UNIQUE (Id, DealershipId)` supports the Appointment composite dealership-bound resource foreign
  key.
- `INDEX (DealershipId, IsActive, Id)` supports active lowest-ID resource selection.

## 6. ServiceBay

| Column | Type | Nullability | Key/constraint | Meaning |
|---|---|---|---|---|
| `Id` | `bigint IDENTITY` | NOT NULL | Primary key; participates in `UNIQUE (Id, DealershipId)` | Service bay identifier. |
| `DealershipId` | `bigint` | NOT NULL | Foreign key to `Dealership`; participates in `UNIQUE (Id, DealershipId)` | Sole dealership ownership. |
| `Name` | `nvarchar(100)` | NOT NULL | — | Service bay name. |
| `IsActive` | `bit` | NOT NULL | — | Eligibility flag. |
| `CreatedAtUtc` | `datetime2(7)` | NOT NULL | — | Creation timestamp in UTC. |

Indexes and constraints:

- `UNIQUE (Id, DealershipId)` supports the Appointment composite dealership-bound resource foreign
  key.
- `INDEX (DealershipId, IsActive, Id)` supports active lowest-ID resource selection.

## 7. TechnicianQualification

| Column | Type | Nullability | Key/constraint | Meaning |
|---|---|---|---|---|
| `TechnicianId` | `bigint` | NOT NULL | Foreign key to `Technician`; part of composite primary key | Technician. |
| `ServiceTypeId` | `bigint` | NOT NULL | Foreign key to `ServiceType`; part of composite primary key | Qualified service. |

Constraints and indexes:

- Composite primary key: `(TechnicianId, ServiceTypeId)`.
- Reverse lookup index: `(ServiceTypeId, TechnicianId)`.
- No direct Dealership-to-ServiceType relationship is introduced; dealership support is derived from
  active technicians and bays connected through their dealership-owned resources.

## 8. BayServiceCompatibility

| Column | Type | Nullability | Key/constraint | Meaning |
|---|---|---|---|---|
| `ServiceBayId` | `bigint` | NOT NULL | Foreign key to `ServiceBay`; part of composite primary key | Compatible bay. |
| `ServiceTypeId` | `bigint` | NOT NULL | Foreign key to `ServiceType`; part of composite primary key | Compatible service. |

Constraints and indexes:

- Composite primary key: `(ServiceBayId, ServiceTypeId)`.
- Reverse lookup index: `(ServiceTypeId, ServiceBayId)`.
- Compatibility is many-to-many and does not create a DealershipServiceType catalog.

## 9. Appointment

| Column | Type | Nullability | Key/constraint | Meaning |
|---|---|---|---|---|
| `Id` | `bigint IDENTITY` | NOT NULL | Primary key | Appointment identifier. |
| `CustomerId` | `bigint` | NOT NULL | Foreign key to `Customer` | Requesting/owning customer. |
| `VehicleId` | `bigint` | NOT NULL | Part of composite foreign key `(VehicleId, CustomerId)` | Booked vehicle. |
| `DealershipId` | `bigint` | NOT NULL | Foreign key to `Dealership`; part of resource composite FKs | Requested dealership. |
| `ServiceTypeId` | `bigint` | NOT NULL | Foreign key to `ServiceType` | Booked service. |
| `TechnicianId` | `bigint` | NULL | Part of `(TechnicianId, DealershipId)` foreign key | Assigned technician. |
| `ServiceBayId` | `bigint` | NULL | Part of `(ServiceBayId, DealershipId)` foreign key | Assigned bay. |
| `ScheduledStartUtc` | `datetime2(7)` | NOT NULL | Whole-minute check | UTC start instant. |
| `ScheduledEndUtc` | `datetime2(7)` | NOT NULL | `ScheduledEndUtc > ScheduledStartUtc` | UTC end instant. |
| `DurationMinutes` | `int` | NOT NULL | `CHECK (DurationMinutes > 0)` | Snapshotted elapsed duration. |
| `Status` | `varchar(32)` | NOT NULL | `IN (Confirmed, Cancelled, Completed)` | Appointment lifecycle state. |
| `AssignmentStatus` | `varchar(32)` | NOT NULL | `IN (Assigned, NeedsReassignment)` | Resource assignment state. |
| `ActualCompletedAtUtc` | `datetime2(7)` | NULL | Completed iff non-null | Completion timestamp in UTC. |
| `CreatedAtUtc` | `datetime2(7)` | NOT NULL | — | Creation timestamp in UTC. |

Foreign keys:

- `CustomerId → Customer`.
- `(VehicleId, CustomerId) → Vehicle(Id, CustomerId)`.
- `DealershipId → Dealership`.
- `ServiceTypeId → ServiceType`.
- `(TechnicianId, DealershipId) → Technician(Id, DealershipId)`.
- `(ServiceBayId, DealershipId) → ServiceBay(Id, DealershipId)`.

Checks:

- `ScheduledEndUtc > ScheduledStartUtc`.
- `DurationMinutes > 0`.
- `ScheduledStartUtc` is whole-minute aligned.
- `Status IN (Confirmed, Cancelled, Completed)`.
- `AssignmentStatus IN (Assigned, NeedsReassignment)`.
- `Assigned` requires both `TechnicianId` and `ServiceBayId`.
- `NeedsReassignment` requires at least one missing resource ID.
- `Completed` if and only if `ActualCompletedAtUtc IS NOT NULL`.

P0 and lifecycle rules:

- P0 creates only `Status = Confirmed` and `AssignmentStatus = Assigned` appointments.
- Nullable resource IDs and `Cancelled`, `Completed`, and `NeedsReassignment` values anticipate
  selected P1 lifecycle states; P0 does not implement those workflows.
- There are no foreign keys from appointments to mutable qualification or compatibility rows.
- There is no database overlap exclusion constraint or trigger in P0.

Indexes supporting conflict searches:

- Index on `VehicleId` with `ScheduledStartUtc`.
- Index on `TechnicianId` with `ScheduledStartUtc`.
- Index on `ServiceBayId` with `ScheduledStartUtc`.
- Index on `DealershipId` with `ScheduledStartUtc`.
- Index on `CustomerId` with `ScheduledStartUtc`.

The application uses half-open intervals `[ScheduledStartUtc, ScheduledEndUtc)`. An overlap exists
when `existing.Start < requested.End` and `existing.End > requested.Start`. Vehicle checks span all
dealerships.

## 10. IdempotencyRecord

| Column | Type | Nullability | Key/constraint | Meaning |
|---|---|---|---|---|
| `Key` | `varchar(128)` | NOT NULL | Primary key; global scope | Client idempotency key. |
| `RequestHash` | `binary(32)` | NOT NULL | SHA-256 canonical request hash | Typed request identity hash. |
| `AppointmentId` | `bigint` | NOT NULL | Foreign key to `Appointment`; filtered unique index | Successful appointment. |
| `ResponseStatusCode` | `smallint` | NOT NULL | — | Saved successful HTTP status. |
| `ResponseBody` | `nvarchar(max)` | NOT NULL | — | Saved successful response representation. |
| `CreatedAtUtc` | `datetime2(7)` | NOT NULL | — | Record creation time in UTC. |
| `ExpiresAtUtc` | `datetime2(7)` | NOT NULL | `CHECK (ExpiresAtUtc > CreatedAtUtc)` | Expiration time in UTC. |

Indexes and constraints:

- `Key` is the global primary key.
- `AppointmentId` has a filtered unique index.
- Index on `ExpiresAtUtc` supports expiration checks.
- There is no `Status` column.
- `AppointmentId → Appointment` uses non-cascading delete behavior.

The canonical request identity consists of typed customerId, vehicleId, dealershipId, serviceTypeId,
and normalized UTC start instant. JSON property order does not affect the identity, and equivalent
offset representations of the same instant produce the same identity. `RequestHash` stores the
SHA-256 hash of that canonical identity.

## Relationships

```text
Customer 1 ──── * Vehicle
Customer 1 ──── * Appointment
Vehicle  1 ──── * Appointment
Dealership 1 ── * Technician
Dealership 1 ── * ServiceBay
Dealership 1 ── * Appointment
ServiceType 1 ─ * Appointment
Technician * ─── * ServiceType       via TechnicianQualification
ServiceBay * ─── * ServiceType       via BayServiceCompatibility
Technician 1 ─── * Appointment       within its dealership
ServiceBay 1 ─── * Appointment       within its dealership
Appointment 1 → 0..1 IdempotencyRecord at the record level, because AppointmentId has a unique index
```

The Appointment composite foreign keys ensure that a vehicle belongs to the appointment customer and
that an assigned technician and bay belong to the appointment dealership. No direct catalog relation
between Dealership and ServiceType exists.

## Atomic Expired-Key Replacement

Expired-key reuse occurs inside the same SQL Server transaction that holds the Idempotency Key,
Vehicle, and Dealership application locks in the approved order. The transaction may replace the
expired `IdempotencyRecord` for the key with the new request hash, appointment ID, response, and new
expiration. The original appointment is never deleted or modified as part of key reuse. If any part of
replacement or the new booking fails, the transaction rolls back and preserves the previous idempotency
record and original appointment.

There is no separately committed Processing state. A successful appointment and its successful
idempotency record are committed atomically. If the commit outcome is uncertain, the server does not
assume rollback; the client retries with the original key to recover the saved successful response.

## P0 Integrity and Implementation Boundaries

- SQL Server uses READ COMMITTED isolation with `READ_COMMITTED_SNAPSHOT OFF`.
- Booking writers acquire transaction-owned exclusive `sp_getapplock` locks in the order
  Idempotency Key → Vehicle → Dealership.
- Each lock acquisition has a five-second timeout.
- Business rules and conflict checks are revalidated after locks are acquired.
- No automatic server-side transaction retries are used.
- Application locks protect integrity only when booking writers follow the protocol. Direct database
  writes that bypass the protocol are a known limitation.
- No overlap trigger or database exclusion constraint is planned for P0.
- P0 creates only fully assigned Confirmed appointments and read operations; nullable fields and
  selected lifecycle values anticipate P1 schema needs only.
