# Feature Specification: Service Booking

**Feature Branch**: `001-service-booking`

**Created**: 2026-11-01

**Status**: Approved requirements, pending repository verification

**Input**: Approved ServiceForge P0 feature requirements for backend-only automotive service scheduling.

**Story Priority Note**: `P1` in the user story headings follows the Spec Kit convention for a
high-priority user story. It does not refer to ServiceForge's deferred P1 implementation scope;
the requirements in this document remain part of the approved P0 feature.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Book a Service Appointment (Priority: P1)

A customer books automotive service for an owned vehicle at a selected dealership and start time.
The service is assigned automatically to one qualified technician and one compatible service bay.

**Why this priority**: Booking a complete appointment is the primary P0 customer value and is the
foundation for all other feature behavior.

**Independent Test**: With valid reference data and an owned vehicle, submit a booking request and
verify that one fully assigned Confirmed appointment is created without partial persistence.

**Acceptance Scenarios**:

1. **Given** an existing customer, owned vehicle, dealership, service type, and valid future start
   time with eligible resources, **When** the customer submits a booking, **Then** the system creates
   one Confirmed appointment with AssignmentStatus Assigned, exactly one technician, exactly one bay,
   and UTC start and end timestamps.
2. **Given** the selected vehicle belongs to another customer, **When** the requesting customer submits
   a booking, **Then** the request is rejected with `VEHICLE_NOT_OWNED` and no appointment is created.
3. **Given** a booking cannot complete all required validations or assignments, **When** the request is
   processed, **Then** no partial appointment is persisted.

---

### User Story 2 - Allocate Eligible Resources (Priority: P1)

The system automatically selects eligible resources at the requested dealership for the requested
service type, choosing the lowest-ID available technician and bay independently.

**Why this priority**: Automatic allocation makes the appointment complete while preserving dealership
resource boundaries and predictable selection behavior.

**Independent Test**: Seed qualified and compatible resources with controlled occupancy and verify the
selected resource IDs and the applicable business error when no resource can be assigned.

**Acceptance Scenarios**:

1. **Given** multiple active qualified technicians and compatible active bays at the dealership,
   **When** a booking is submitted, **Then** the lowest-ID available technician and lowest-ID available
   bay are selected independently.
2. **Given** the lowest-ID eligible technician is occupied for part of the requested interval,
   **When** a booking is submitted, **Then** the next available eligible technician is selected.
3. **Given** the lowest-ID eligible bay is occupied for part of the requested interval, **When** a
   booking is submitted, **Then** the next available eligible bay is selected.
4. **Given** no active qualified technician exists, **When** a booking is submitted, **Then** the
   request is rejected with `NO_QUALIFIED_TECHNICIAN`.
5. **Given** no active compatible bay exists, **When** a booking is submitted, **Then** the request is
   rejected with `NO_COMPATIBLE_BAY`.
6. **Given** eligible resources exist but all required resources are occupied for the full interval,
   **When** a booking is submitted, **Then** the request is rejected with `NO_AVAILABLE_RESOURCES`.

---

### User Story 3 - Validate Booking Time (Priority: P1)

The customer provides an explicit-offset start time, and the system validates the represented instant,
dealership-local operating day, operating hours, duration, daylight-saving behavior, and booking horizon.

**Why this priority**: Correct time validation prevents invalid appointments and ensures that elapsed
service duration is interpreted consistently across time zones and daylight-saving transitions.

**Independent Test**: Submit bookings at boundary, weekend, horizon, offset, and daylight-saving cases
and verify acceptance or the specified business error.

**Acceptance Scenarios**:

1. **Given** a start time on a Monday through Friday within dealership-local hours, **When** the entire
   elapsed appointment ends by 17:00 local time, **Then** the time is accepted.
2. **Given** an appointment whose local end time is exactly 17:00, **When** the request is validated,
   **Then** it is accepted.
3. **Given** a Saturday, Sunday, or time outside 09:00–17:00 Monday–Friday, **When** a booking is
   submitted, **Then** it is rejected with `OUTSIDE_OPERATING_HOURS`.
4. **Given** a start time with nonzero seconds or fractional seconds, **When** a booking is submitted,
   **Then** it is rejected with `INVALID_BOOKING_TIME`.
5. **Given** a start time that is not strictly later than authoritative SQL Server current UTC time,
   **When** a booking is submitted, **Then** it is rejected with `INVALID_BOOKING_TIME` without rounding
   the current time.
6. **Given** a start date within one dealership-local calendar year from the current local date,
   **When** a booking is submitted, **Then** the date is within the inclusive horizon.
7. **Given** the current local date is February 29 and the following year lacks February 29, **When**
   the horizon is calculated, **Then** February 28 of the following year is the inclusive horizon date.
8. **Given** a requested start time without an explicit ISO 8601 UTC offset, **When** a booking is
   submitted, **Then** it is rejected with `INVALID_BOOKING_TIME`.
9. **Given** an explicit offset that differs from the dealership timezone's offset, **When** a booking
   is submitted, **Then** the represented instant is authoritative and the dealership timezone is used
   for local operating-hours and horizon validation.
10. **Given** a service type with a positive globally defined duration, **When** the booking is created,
    **Then** the end instant is calculated by adding elapsed duration to the UTC start instant, local
    business validation includes daylight-saving transitions, and the duration is snapshotted on the
    appointment.

---

### User Story 4 - Prevent Conflicting Bookings (Priority: P1)

The system prevents overlapping occupied intervals for a vehicle, technician, or bay while permitting
back-to-back appointments.

**Why this priority**: Conflict prevention protects customers and dealership operations when bookings
are created concurrently or across dealerships.

**Independent Test**: Submit controlled overlapping and adjacent bookings, including concurrent
requests, and verify the accepted result, conflict response, and absence of partial persistence.

**Acceptance Scenarios**:

1. **Given** an existing occupied interval, **When** a new interval overlaps it for the same vehicle,
   technician, or bay, **Then** the new booking is rejected and the applicable resource remains
   unchanged.
2. **Given** an existing appointment ending at the requested start, **When** the new appointment starts
   at that exact instant, **Then** the back-to-back booking is permitted.
3. **Given** the same vehicle has an occupied appointment at another dealership, **When** an overlapping
   booking is submitted, **Then** it is rejected with `VEHICLE_TIME_CONFLICT`.
4. **Given** two concurrent requests could allocate the same occupied resource or vehicle interval,
   **When** both are processed, **Then** at most one succeeds when both requests cannot be accommodated
   without violating vehicle or resource availability constraints, and all business rules are
   revalidated under transaction protection.
5. **Given** a booking transaction fails, **When** processing ends, **Then** no partial appointment or
   successful idempotency result remains.

---

### User Story 5 - Retry Safely Using Idempotency (Priority: P1)

The customer can safely retry booking creation using a required global idempotency key without creating
duplicate appointments or changing the original appointment.

**Why this priority**: Durable idempotency protects customers from duplicate bookings when responses are
lost or requests are retried under concurrency.

**Independent Test**: Repeat identical, equivalent-offset, conflicting-payload, concurrent, expired-key,
and lost-response requests and verify the specified response and persistence behavior.

**Acceptance Scenarios**:

1. **Given** a successful booking created with an unexpired key, **When** the same key and canonical
   request are submitted again, **Then** the saved successful response is replayed.
2. **Given** an unexpired key is reused with a different canonical request, **When** the request is
   submitted, **Then** it is rejected with `IDEMPOTENCY_KEY_CONFLICT`.
3. **Given** equivalent offset representations of the same instant and the same typed request values,
   **When** the same key is retried, **Then** both requests have the same canonical request identity.
4. **Given** concurrent requests use the same key, **When** the idempotency lock cannot be acquired
   within its timeout, **Then** the request returns `REQUEST_IN_PROGRESS` with `Retry-After: 2` seconds.
5. **Given** a key has expired after 24 hours from creation, **When** it is reused, **Then** it can be
   used for a new booking and the original appointment is not deleted or modified.
6. **Given** a booking request fails, **When** processing ends, **Then** no successful idempotency
   result is persisted.
7. **Given** a client loses the response after an uncertain commit outcome, **When** it retries with
   the original key, **Then** the result can be recovered without assuming rollback.

---

### User Story 6 - Retrieve Appointments and Reference Data (Priority: P1)

Customers and consumers can retrieve appointments and reference data through the approved collection
and appointment endpoints.

**Why this priority**: Read operations expose the confirmed booking and the reference data needed to
select booking inputs while remaining within P0 scope.

**Independent Test**: Call each required endpoint with found, absent, paginated, and invalid requests
and verify status codes, representations, ordering, and pagination metadata.

**Acceptance Scenarios**:

1. **Given** a valid booking request, **When** it is created, **Then** `POST /api/appointments`
   returns 201 Created, a Location header, and a confirmed appointment representation with assigned
   resource IDs and UTC timestamps.
2. **Given** an existing appointment ID, **When** `GET /api/appointments/{id}` is called, **Then** it
   returns 200 and the appointment representation.
3. **Given** an absent appointment ID, **When** `GET /api/appointments/{id}` is called, **Then** it
   returns 404 with `RESOURCE_NOT_FOUND`.
4. **Given** reference data exists, **When** the customer calls the vehicle, dealership, or service
   type collection endpoint, **Then** the response uses the required offset pagination metadata and
   stable ID-ascending order.
5. **Given** a collection request omits pagination values, **When** it is processed, **Then** page 1
   and page size 20 are used; a requested page size above 100 is rejected as invalid.

### Edge Cases

- Unknown customer, vehicle, dealership, or service type is rejected as `RESOURCE_NOT_FOUND`.
- A vehicle can have only one owner, while one customer can own multiple vehicles.
- A customer may book different vehicles at overlapping times.
- The same vehicle cannot overlap across dealerships, but back-to-back appointments are allowed.
- A dealership supports a service when it has at least one active qualified technician and at least
  one active compatible bay, regardless of current occupancy.
- There is no holiday calendar in P0.
- There is no minimum advance-notice period.
- Start times may be any whole minute, but the entire elapsed appointment must fit within one local
  operating day.
- A supplied offset may differ from the dealership timezone offset; its represented instant remains
  authoritative.
- Daylight-saving transitions must use elapsed duration rather than wall-clock arithmetic.
- SQL Server current UTC is authoritative for future-time and idempotency-expiration decisions.
- An uncertain transaction outcome must not be treated as a confirmed rollback.

## Requirements *(mandatory)*

### Functional Requirements

#### Booking and Ownership

- **FR-001**: The system MUST allow a customer to request an appointment using a customer, owned
  vehicle, dealership, service type, and start time.
- **FR-002**: The system MUST require the customer, vehicle, dealership, and service type to exist.
- **FR-003**: Each vehicle MUST belong to exactly one customer, and a customer MUST be able to own
  multiple vehicles.
- **FR-004**: The system MUST validate that the vehicle belongs to the requesting customer.
- **FR-005**: A successful P0 appointment MUST have Status `Confirmed` and AssignmentStatus `Assigned`.
- **FR-006**: Each successful appointment MUST have exactly one assigned technician and exactly one
  assigned service bay.
- **FR-007**: Resource assignment MUST be automatic; customers MUST NOT choose resources.
- **FR-008**: The system MUST NOT persist a partial booking.

#### Resource Eligibility and Allocation

- **FR-009**: Every technician and service bay MUST belong to exactly one dealership.
- **FR-010**: Technician-to-ServiceType qualification MUST support many-to-many relationships.
- **FR-011**: ServiceBay-to-ServiceType compatibility MUST support many-to-many relationships.
- **FR-012**: An eligible technician MUST be active, belong to the requested dealership, and be
  qualified for the requested service.
- **FR-013**: An eligible bay MUST be active, belong to the requested dealership, and be compatible
  with the requested service.
- **FR-014**: The system MUST select the lowest-ID available eligible technician and independently
  select the lowest-ID available eligible bay.
- **FR-015**: If the lowest-ID eligible resource is occupied, the system MUST consider the next
  available eligible resource.
- **FR-016**: Availability MUST cover the entire requested appointment interval.
- **FR-017**: The system MUST NOT create a direct Dealership-to-ServiceType catalog or relationship.
- **FR-018**: A dealership MUST be considered to support a service when it has at least one active
  qualified technician and at least one active compatible bay, regardless of occupancy.
- **FR-019**: If no active qualified technician exists, the system MUST return
  `NO_QUALIFIED_TECHNICIAN`.
- **FR-020**: If no active compatible bay exists, the system MUST return `NO_COMPATIBLE_BAY`.
- **FR-021**: If eligible resources exist but required resources are occupied, the system MUST return
  `NO_AVAILABLE_RESOURCES`.
- **FR-022**: The system MUST NOT introduce `SERVICE_NOT_OFFERED`.

#### Time and Duration

- **FR-023**: Dealership operating hours MUST be Monday–Friday, 09:00–17:00 local time; Saturday and
  Sunday MUST be closed.
- **FR-024**: The system MUST NOT use a holiday calendar in P0.
- **FR-025**: The entire appointment MUST fit within one dealership-local operating day, and an end
  time exactly at 17:00 MUST be valid.
- **FR-026**: Start times MUST be on whole minutes; nonzero seconds or fractional seconds MUST be
  rejected with `INVALID_BOOKING_TIME`.
- **FR-027**: The start instant MUST be strictly later than authoritative SQL Server current UTC time,
  without rounding.
- **FR-028**: The system MUST NOT impose a minimum advance-notice period.
- **FR-029**: The maximum booking horizon MUST be one inclusive dealership-local calendar year based
  on date; February 29 MUST map to February 28 when the following year lacks February 29.
- **FR-030**: A booking start time MUST use ISO 8601 with an explicit UTC offset.
- **FR-031**: The supplied offset MUST NOT be required to equal the dealership timezone offset; the
  represented instant is authoritative.
- **FR-032**: The dealership timezone MUST determine operating-hours and horizon validation.
- **FR-033**: ServiceType.DurationMinutes MUST be globally defined and positive.
- **FR-034**: Service duration MUST represent elapsed real time, and the end time MUST be calculated by
  adding duration to the UTC start instant.
- **FR-035**: The system MUST convert start and end to dealership-local time for business validation,
  including daylight-saving transitions.
- **FR-036**: Appointment start and end timestamps MUST be persisted and returned in UTC.
- **FR-037**: The service duration MUST be snapshotted into the appointment at creation.

#### Conflicts and Transactions

- **FR-038**: Appointment intervals MUST use half-open boundaries `[start, end)`.
- **FR-039**: An overlap MUST exist when `existing.Start < requested.End` AND
  `existing.End > requested.Start`.
- **FR-040**: Back-to-back appointments MUST be permitted, including for the same vehicle at different
  dealerships.
- **FR-041**: The system MUST prevent occupied interval overlap for vehicles, technicians, and bays.
- **FR-042**: Vehicle conflict checks MUST span all dealerships.
- **FR-043**: Booking operations MUST be atomic and MUST revalidate availability under transaction
  protection.
- **FR-044**: Concurrent requests MUST NOT both successfully allocate the same occupied resource or
  vehicle interval.
- **FR-045**: SQL Server booking transactions MUST use READ COMMITTED isolation with
  READ_COMMITTED_SNAPSHOT OFF.
- **FR-046**: Booking transactions MUST use transaction-owned exclusive `sp_getapplock` locks.
- **FR-047**: Locks MUST be acquired in this strict global order: Idempotency Key → Vehicle →
  Dealership.
- **FR-048**: Each lock acquisition MUST have a five-second timeout.
- **FR-049**: Business rules and conflicts MUST be revalidated after locks are acquired.
- **FR-050**: Automatic server-side transaction retries MUST NOT be used.
- **FR-051**: An uncertain commit outcome MUST NOT be assumed to be a rollback.
- **FR-052**: A vehicle or dealership lock timeout or SQL deadlock MUST return
  `BOOKING_TEMPORARILY_UNAVAILABLE`.
- **FR-053**: The appointment and successful idempotency response MUST be committed atomically.
- **FR-054**: Application locks MUST protect integrity only when booking writers follow the protocol;
  SQL overlap triggers are not planned for P0.

#### Idempotency

- **FR-055**: `POST /api/appointments` MUST require `Idempotency-Key`.
- **FR-056**: The idempotency key scope MUST be global.
- **FR-057**: Successful results MUST be retained for 24 hours from creation.
- **FR-058**: Canonical request identity MUST consist of typed customerId, vehicleId, dealershipId,
  serviceTypeId, and normalized UTC start instant.
- **FR-059**: JSON property order MUST NOT affect request identity.
- **FR-060**: Equivalent offset representations of the same instant MUST produce the same identity.
- **FR-061**: The same unexpired key with the same canonical request MUST replay the saved successful
  response.
- **FR-062**: The same unexpired key with a different canonical request MUST return
  `IDEMPOTENCY_KEY_CONFLICT`.
- **FR-063**: Concurrent requests using the same key MUST be serialized.
- **FR-064**: If the idempotency lock cannot be acquired within its timeout, the system MUST return
  `REQUEST_IN_PROGRESS`.
- **FR-065**: An expired key MAY be reused for a new booking, and reuse MUST NOT delete or modify the
  original appointment.
- **FR-066**: Failed booking requests MUST NOT persist successful idempotency results.
- **FR-067**: There MUST be no separately committed Processing state, and IdempotencyRecord MUST have
  no Status column.
- **FR-068**: A client that loses the response after an uncertain commit outcome MUST be able to retry
  using the original key without assuming rollback.
- **FR-069**: Idempotency-lock timeout MUST return HTTP 409 `REQUEST_IN_PROGRESS`.

#### API and Reference Data

- **FR-070**: The system MUST provide `POST /api/appointments`.
- **FR-071**: The system MUST provide `GET /api/appointments/{id}`.
- **FR-072**: The system MUST provide `GET /api/customers/{id}/vehicles`.
- **FR-073**: The system MUST provide `GET /api/dealerships`.
- **FR-074**: The system MUST provide `GET /api/service-types`.
- **FR-075**: Successful appointment creation MUST return HTTP 201 Created, a Location header, and a
  confirmed appointment representation containing assigned resource IDs and UTC timestamps.
- **FR-076**: An existing appointment retrieval MUST return HTTP 200; an absent appointment MUST return
  HTTP 404.
- **FR-077**: Collection endpoints MUST use offset pagination with page 1-based and defaulting to 1,
  pageSize defaulting to 20 and capped at 100, stable ID-ascending ordering, and pagination applied
  in the database.
- **FR-078**: Collection responses MUST include `items`, `page`, `pageSize`, `totalCount`, and
  `totalPages`.
- **FR-079**: Reference data MUST be seeded for the demonstration.
- **FR-080**: API errors MUST use RFC 9457 Problem Details with a machine-readable business error code
  extension and field-level validation details where applicable.
- **FR-081**: HTTP 400 errors MUST support `INVALID_REQUEST`, `INVALID_BOOKING_TIME`,
  `OUTSIDE_OPERATING_HOURS`, and `BOOKING_HORIZON_EXCEEDED`.
- **FR-082**: HTTP 403 errors MUST support `VEHICLE_NOT_OWNED`.
- **FR-083**: HTTP 404 errors MUST support `RESOURCE_NOT_FOUND`.
- **FR-084**: HTTP 409 errors MUST support `VEHICLE_TIME_CONFLICT`, `NO_QUALIFIED_TECHNICIAN`,
  `NO_COMPATIBLE_BAY`, `NO_AVAILABLE_RESOURCES`, `IDEMPOTENCY_KEY_CONFLICT`, and
  `REQUEST_IN_PROGRESS`.
- **FR-085**: HTTP 503 errors MUST support `BOOKING_TEMPORARILY_UNAVAILABLE`.
- **FR-086**: `REQUEST_IN_PROGRESS` and `BOOKING_TEMPORARILY_UNAVAILABLE` responses MUST include
  `Retry-After: 2` seconds.

#### Quality and Scope

- **FR-087**: The feature MUST have unit tests for deterministic rules.
- **FR-088**: The feature MUST have xUnit API integration tests with WebApplicationFactory.
- **FR-089**: Database integration tests MUST use a real SQL Server through the dedicated disposable
  integration-test database configured by `SERVICEFORGE_TEST_SQLSERVER_CONNECTION`; Testcontainers
  MAY be used where explicitly approved and available.
- **FR-090**: Concurrency tests MUST use separate connections and controlled synchronization.
- **FR-091**: Tests MUST cover idempotency, rollback, timezone, ownership, resource allocation, and
  HTTP contracts.
- **FR-092**: EF Core InMemory MUST NOT be treated as proof of SQL Server concurrency behavior.
- **FR-093**: Swagger/OpenAPI MUST document the implemented endpoints.
- **FR-094**: Test success MUST NOT be claimed unless the tests were executed successfully.
- **FR-095**: P0 MUST NOT include authentication or JWT, Angular, Playwright, payments, appointment
  rescheduling, appointment cancellation/completion endpoints, technician leave or holiday calendars,
  resource inactivation or reassignment workflows, notifications, customer travel time, resource
  buffers or scheduling optimization, overrun recovery, warranty workflows, an admin or technician UI,
  microservices, or message queues.
- **FR-096**: P0 MUST implement only creation of fully assigned Confirmed appointments and read
  operations; P1 workflows remain deferred. Selected P1 lifecycle states MAY be anticipated by the
  approved database design but MUST NOT be implemented as P0 workflows.

### Key Entities

- **Customer**: The customer requesting service; may own multiple vehicles.
- **Vehicle**: A vehicle owned by exactly one customer and subject to cross-dealership appointment
  conflict checks.
- **Dealership**: A dealership with a local timezone, operating hours, technicians, and service bays.
- **ServiceType**: A globally defined service with a positive duration in minutes.
- **Technician**: An active dealership resource with many-to-many service qualifications.
- **ServiceBay**: An active dealership resource with many-to-many service compatibilities.
- **Appointment**: A fully assigned Confirmed service booking with UTC timestamps and a snapshotted
  service duration.
- **IdempotencyRecord**: A successful booking response retained for 24 hours and associated with a
  global key and canonical request identity; it has no Status column.

## API Surface and Error Behavior

The P0 API surface consists only of the five required endpoints:

| Method | Path | Successful response |
|---|---|---|
| POST | `/api/appointments` | 201 Created, Location header, confirmed appointment |
| GET | `/api/appointments/{id}` | 200 when found |
| GET | `/api/customers/{id}/vehicles` | 200 paginated collection |
| GET | `/api/dealerships` | 200 paginated collection |
| GET | `/api/service-types` | 200 paginated collection |

All error responses use RFC 9457 Problem Details with a machine-readable code extension. The approved
status-code mappings are:

| HTTP status | Codes |
|---|---|
| 400 | `INVALID_REQUEST`, `INVALID_BOOKING_TIME`, `OUTSIDE_OPERATING_HOURS`, `BOOKING_HORIZON_EXCEEDED` |
| 403 | `VEHICLE_NOT_OWNED` |
| 404 | `RESOURCE_NOT_FOUND` |
| 409 | `VEHICLE_TIME_CONFLICT`, `NO_QUALIFIED_TECHNICIAN`, `NO_COMPATIBLE_BAY`, `NO_AVAILABLE_RESOURCES`, `IDEMPOTENCY_KEY_CONFLICT`, `REQUEST_IN_PROGRESS` |
| 503 | `BOOKING_TEMPORARILY_UNAVAILABLE` |

`REQUEST_IN_PROGRESS` and `BOOKING_TEMPORARILY_UNAVAILABLE` include `Retry-After: 2` seconds.

## Reliability Constraints

Booking reliability requirements are mandatory but are stated as observable constraints rather than
an implementation tutorial:

- SQL Server uses READ COMMITTED isolation with READ_COMMITTED_SNAPSHOT OFF.
- Booking transactions use transaction-owned exclusive `sp_getapplock` locks.
- Locks follow the strict order Idempotency Key → Vehicle → Dealership.
- Each lock acquisition has a five-second timeout.
- Business rules, resource availability, and conflicts are revalidated under transaction protection.
- There are no automatic server-side transaction retries.
- Idempotency-lock timeout returns HTTP 409 `REQUEST_IN_PROGRESS`.
- Vehicle/dealership lock timeout or SQL deadlock returns HTTP 503
  `BOOKING_TEMPORARILY_UNAVAILABLE`.
- Successful appointment and idempotency record persistence is atomic.
- SQL Server UTC is authoritative for future-time and idempotency-expiration decisions.
- Application locks protect integrity only when booking writers follow the protocol; no SQL overlap
  triggers are planned for P0.

## Testable Acceptance Scenarios

The following scenario set is mandatory for verification:

1. Successful booking with automatic assignment.
2. Lowest-ID eligible technician is busy; next available technician is selected.
3. Lowest-ID eligible bay is busy; next available bay is selected.
4. Missing technician qualification.
5. Missing bay compatibility.
6. Eligible resources exist but are all occupied.
7. Vehicle ownership mismatch.
8. Same vehicle overlapping at different dealerships.
9. Different vehicles owned by the same customer booked simultaneously.
10. Back-to-back intervals.
11. Start time exactly at a minute boundary but already in the past relative to SQL Server now.
12. End time exactly at 17:00.
13. Weekend or outside-hours booking.
14. Inclusive one-year horizon and February 29 edge case.
15. DST transition and elapsed-time duration.
16. Concurrent overlapping booking attempts.
17. Idempotency replay with equivalent offsets.
18. Idempotency key reused with different payload.
19. Concurrent same-key contention.
20. Expired key reuse preserving the original appointment.
21. Transaction rollback without partial persistence.
22. Lost response followed by retry using the original key.
23. Collection pagination and stable ordering.
24. Correct Problem Details responses.

## Explicit Exclusions

The following are excluded from P0 implementation requirements:

- Authentication or JWT
- Angular frontend
- Playwright
- Payments
- Appointment rescheduling
- Appointment cancellation or completion endpoints
- Technician leave or holiday calendars
- Resource inactivation or reassignment workflows
- Notifications
- Customer travel time
- Resource buffers or scheduling optimization
- Overrun recovery
- Warranty workflows
- Admin or technician UI
- Microservices or message queues

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Every accepted valid booking produces exactly one fully assigned Confirmed appointment,
  with one technician, one service bay, and UTC start and end timestamps.
- **SC-002**: No testable booking result contains overlapping occupied intervals for a vehicle,
  technician, or bay, including under concurrent booking attempts.
- **SC-003**: A repeated successful request using the same unexpired key returns the saved successful
  result and creates no duplicate appointment for the 24-hour retention period.
- **SC-004**: Every listed invalid, conflicting, unavailable, or in-progress booking condition returns
  its approved HTTP status and machine-readable error code.
- **SC-005**: All five P0 endpoints return the specified response shape, status behavior, stable
  ordering, and pagination metadata for valid and invalid collection requests.
- **SC-006**: The complete mandatory set of 24 acceptance scenarios is executable as reviewable tests,
  including timezone, daylight-saving, ownership, allocation, concurrency, rollback, idempotency, and
  HTTP error behavior.

## Assumptions

- The P0 system is backend-only and serves customers and consumers through the approved REST endpoints.
- Reference data is seeded for demonstration and includes customers, vehicles, dealerships, service
  types, technicians, bays, qualifications, and compatibilities sufficient to exercise the scenarios.
- The authoritative dealership timezone is available for each dealership and is used for local
  operating-hours and horizon validation.
- SQL Server current UTC is the authoritative clock for future-time and idempotency-expiration checks.
- No authentication or authorization mechanism is part of P0; vehicle ownership validation uses the
  requesting customer identity supplied by the approved request contract.
- Selected P1 lifecycle states may be represented in the approved database design, but P0 creates only
  fully assigned Confirmed appointments and exposes only the required read operations.
