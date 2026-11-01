using ServiceForge.Application.Abstractions;
using ServiceForge.Application.Appointments.Allocation;
using ServiceForge.Application.Appointments.Rules;
using ServiceForge.Domain.Entities;
using ServiceForge.Domain.Enums;
using ServiceForge.Domain.Time;

namespace ServiceForge.UnitTests;

public sealed class BookingBusinessRulesTests
{
    private static readonly DateTime NowUtc = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Booking_time_normalizes_explicit_offset_to_utc_and_adds_elapsed_duration()
    {
        var validator = new BookingTimeValidator(new FixedClock(NowUtc));

        var result = validator.Validate("2026-01-02T09:00:00+07:00", 90);

        Assert.True(result.IsValid);
        Assert.Equal(new DateTime(2026, 1, 2, 2, 0, 0, DateTimeKind.Utc), result.Value!.Interval.StartUtc);
        Assert.Equal(new DateTime(2026, 1, 2, 3, 30, 0, DateTimeKind.Utc), result.Value.Interval.EndUtc);
    }

    [Theory]
    [InlineData("2026-01-02T09:00:00")]
    [InlineData("2026-01-02T09:00:01+07:00")]
    [InlineData("2025-12-31T09:00:00+07:00")]
    public void Booking_time_rejects_missing_offset_seconds_and_non_future_start(string startTime)
    {
        var validator = new BookingTimeValidator(new FixedClock(NowUtc));

        var result = validator.Validate(startTime, 60);

        Assert.False(result.IsValid);
        Assert.Equal(BookingRuleErrorCodes.InvalidBookingTime, result.ErrorCode);
    }

    [Fact]
    public void Half_open_intervals_allow_back_to_back_appointments()
    {
        var existing = BookingInterval.Create(
            new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc));
        var adjacent = BookingInterval.Create(
            new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 2, 11, 0, 0, DateTimeKind.Utc));
        var overlapping = BookingInterval.Create(
            new DateTime(2026, 1, 2, 9, 59, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 2, 10, 30, 0, DateTimeKind.Utc));

        Assert.False(existing.Overlaps(adjacent));
        Assert.True(existing.Overlaps(overlapping));
    }

    [Fact]
    public void Dealership_hours_allow_exact_seventeen_hundred_end()
    {
        var validator = new DealershipLocalTimeValidator(new FixedClock(NowUtc));
        var interval = BookingInterval.Create(
            new DateTime(2026, 1, 2, 16, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 2, 17, 0, 0, DateTimeKind.Utc));

        var result = validator.Validate(interval, TimeZoneInfo.Utc);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Dealership_hours_reject_weekends_and_cross_day_intervals()
    {
        var validator = new DealershipLocalTimeValidator(new FixedClock(NowUtc));
        var weekend = BookingInterval.Create(
            new DateTime(2026, 1, 3, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 3, 11, 0, 0, DateTimeKind.Utc));
        var crossDay = BookingInterval.Create(
            new DateTime(2026, 1, 2, 23, 30, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 3, 0, 30, 0, DateTimeKind.Utc));

        Assert.Equal(BookingRuleErrorCodes.OutsideOperatingHours, validator.Validate(weekend, TimeZoneInfo.Utc).ErrorCode);
        Assert.Equal(BookingRuleErrorCodes.OutsideOperatingHours, validator.Validate(crossDay, TimeZoneInfo.Utc).ErrorCode);
    }

    [Fact]
    public void Horizon_is_inclusive_and_handles_leap_day_fallback()
    {
        var validator = new DealershipLocalTimeValidator(
            new FixedClock(new DateTime(2028, 2, 29, 8, 0, 0, DateTimeKind.Utc)));
        var horizonDay = BookingInterval.Create(
            new DateTime(2029, 2, 28, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2029, 2, 28, 10, 0, 0, DateTimeKind.Utc));
        var beyondHorizon = BookingInterval.Create(
            new DateTime(2029, 3, 1, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2029, 3, 1, 10, 0, 0, DateTimeKind.Utc));

        Assert.True(validator.Validate(horizonDay, TimeZoneInfo.Utc).IsValid);
        Assert.Equal(BookingRuleErrorCodes.BookingHorizonExceeded, validator.Validate(beyondHorizon, TimeZoneInfo.Utc).ErrorCode);
    }

    [Fact]
    public void Vehicle_rules_enforce_ownership_and_ignore_cancelled_appointments()
    {
        var vehicle = new Vehicle(1, 2, "SF-1", NowUtc);
        var interval = BookingInterval.Create(
            new DateTime(2026, 1, 2, 9, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc));
        var cancelled = new Appointment(1, 2, 1, 1, 1, 1, 1, interval.StartUtc, interval.EndUtc, 60,
            AppointmentStatus.Cancelled, AssignmentStatus.Assigned, null, NowUtc);

        Assert.True(VehicleBookingRules.ValidateOwnership(vehicle, 2).IsValid);
        Assert.Equal(BookingRuleErrorCodes.VehicleNotOwned, VehicleBookingRules.ValidateOwnership(vehicle, 1).ErrorCode);
        Assert.False(VehicleBookingRules.HasOccupiedOverlap(new[] { cancelled }, 1, interval));
    }

    [Fact]
    public void Resource_allocator_selects_lowest_available_ids_independently()
    {
        var eligibility = new ResourceEligibilityResult(true, true, new[] { 8L, 3L }, new[] { 9L, 4L });

        var result = ResourceAllocator.SelectLowestAvailable(eligibility);

        Assert.True(result.IsValid);
        Assert.Equal(3, result.Value!.TechnicianId);
        Assert.Equal(4, result.Value.ServiceBayId);
    }

    [Theory]
    [InlineData(false, true, "NO_QUALIFIED_TECHNICIAN")]
    [InlineData(true, false, "NO_COMPATIBLE_BAY")]
    [InlineData(true, true, "NO_AVAILABLE_RESOURCES")]
    public void Resource_eligibility_preserves_approved_error_distinctions(
        bool hasTechnician,
        bool hasBay,
        string expectedCode)
    {
        var eligibility = new ResourceEligibilityResult(hasTechnician, hasBay, Array.Empty<long>(), Array.Empty<long>());

        Assert.Equal(expectedCode, ResourceEligibilityRules.GetErrorCode(eligibility));
    }

    private sealed class FixedClock(DateTime utcNow) : IAuthoritativeUtcClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
