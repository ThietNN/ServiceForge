# P0 Acceptance Traceability

This matrix traces the 24 mandatory verification scenarios listed under **Testable Acceptance
Scenarios** in `specs/001-service-booking/spec.md`. “Partial” records a deliberate verification
limitation; it is not treated as a passing claim.

| ID | Mandatory scenario | Test evidence | Level | Status |
|---:|---|---|---|---|
| 1 | Successful booking with automatic assignment | `Post_creates_appointment_with_location_and_get_returns_it`; `Different_vehicles_are_assigned_distinct_resources_when_the_dealership_has_capacity` | HTTP / SQL | Covered |
| 2 | Busy lowest-ID technician falls back | `Qualifications_compatibility_activity_and_occupancy_drive_resource_selection` | SQL | Covered |
| 3 | Busy lowest-ID bay falls back | `Qualifications_compatibility_activity_and_occupancy_drive_resource_selection` | SQL | Covered |
| 4 | Missing technician qualification | `Missing_qualification_and_compatibility_are_distinguished_from_occupancy`; `Resource_eligibility_preserves_approved_error_distinctions` | SQL / unit | Covered |
| 5 | Missing bay compatibility | `Missing_qualification_and_compatibility_are_distinguished_from_occupancy`; `Resource_eligibility_preserves_approved_error_distinctions` | SQL / unit | Covered |
| 6 | All eligible resources occupied | `Qualifications_compatibility_activity_and_occupancy_drive_resource_selection`; `Resource_eligibility_preserves_approved_error_distinctions` | SQL / unit | Covered |
| 7 | Vehicle ownership mismatch | `Ownership_failure_and_vehicle_conflict_use_approved_problem_details`; `Vehicle_rules_enforce_ownership_and_ignore_cancelled_appointments` | HTTP / unit | Covered |
| 8 | Same vehicle overlap at another dealership | `Same_vehicle_overlap_is_serialized_across_different_dealerships`; `Vehicle_ownership_and_cross_dealership_conflicts_are_queryable` | SQL | Covered |
| 9 | Different vehicles booked simultaneously | `Different_vehicles_are_assigned_distinct_resources_when_the_dealership_has_capacity` | SQL | Covered |
| 10 | Back-to-back intervals | `Half_open_intervals_allow_back_to_back_appointments`; `Vehicle_ownership_and_cross_dealership_conflicts_are_queryable` | unit / SQL | Covered |
| 11 | Past start relative to SQL Server UTC | `Booking_time_rejects_missing_offset_seconds_and_non_future_start`; `Sql_server_clock_returns_authoritative_utc_time` | unit / SQL | Covered |
| 12 | End exactly at 17:00 | `Dealership_hours_allow_exact_seventeen_hundred_end` | unit | Covered |
| 13 | Weekend or outside-hours rejection | `Dealership_hours_reject_weekends_and_cross_day_intervals` | unit | Covered |
| 14 | Inclusive horizon and February 29 | `Horizon_is_inclusive_and_handles_leap_day_fallback` | unit | Covered |
| 15 | DST transition and elapsed duration | `Dst_transition_preserves_elapsed_utc_duration`; `Booking_time_normalizes_explicit_offset_to_utc_and_adds_elapsed_duration` | unit | Covered |
| 16 | Concurrent overlapping booking attempts | `Different_keys_for_the_same_vehicle_allow_at_most_one_overlapping_booking`; `Same_vehicle_overlap_is_serialized_across_different_dealerships` | SQL | Covered |
| 17 | Idempotency replay with equivalent offsets | `Equivalent_offset_replay_recovers_a_committed_booking_after_response_loss`; `Equivalent_offset_representations_share_the_same_identity` | HTTP / unit | Covered |
| 18 | Idempotency key reused with different payload | `Different_requests_using_the_same_key_return_one_success_and_one_conflict`; `Matching_replay_preserves_body_and_location_and_conflict_returns_problem_details` | SQL / HTTP | Covered |
| 19 | Concurrent same-key contention | `Identical_same_key_requests_create_one_appointment_and_replay_the_same_response`; `Held_idempotency_and_vehicle_locks_map_to_the_approved_transient_outcomes` | SQL | Covered |
| 20 | Expired key reuse preserves original | `Expired_key_reuse_creates_a_new_booking_and_retains_the_original`; `Atomic_persistence_replays_conflicts_replaces_expired_records_and_rolls_back` | SQL | Covered |
| 21 | Rollback leaves no partial persistence | `Failed_booking_does_not_leave_an_idempotency_record_or_orphaned_appointment`; `Atomic_persistence_replays_conflicts_replaces_expired_records_and_rolls_back` | SQL | Covered |
| 22 | Lost response followed by original-key retry | `Equivalent_offset_replay_recovers_a_committed_booking_after_response_loss` verifies database commit before discarding the original response, then retries the equivalent request and checks one appointment/current idempotency record | HTTP / SQL | Covered: deterministic post-commit response-loss simulation; no network failure is fabricated. |
| 23 | Collection pagination and stable ordering | `Validation_and_missing_key_return_problem_details_and_lists_are_paginated` | HTTP | Covered |
| 24 | Correct Problem Details responses | `Every_approved_http_problem_mapping_returns_the_approved_code_and_status` exercises every approved code/status mapping, Problem Details content type, code extension, and Retry-After cases; validation tests cover field errors | HTTP | Covered |

## Regression limitations

- SQL deadlock-victim classification maps to `BOOKING_TEMPORARILY_UNAVAILABLE` without retry, but no
  artificial deadlock is induced because a deterministic deadlock test would risk unrelated data.
- Retry-after behavior is implemented and HTTP-verified for `REQUEST_IN_PROGRESS` and
  `BOOKING_TEMPORARILY_UNAVAILABLE`.
