using ServiceForge.Application.Abstractions;
using ServiceForge.Application.Validation;
using ServiceForge.Domain.Time;

namespace ServiceForge.Application.Appointments.Rules;

public sealed record NormalizedBookingTime(BookingInterval Interval, int DurationMinutes);

public sealed class BookingTimeValidator(IAuthoritativeUtcClock authoritativeUtcClock)
{
    public RuleResult<NormalizedBookingTime> Validate(string? startTime, int durationMinutes)
    {
        if (durationMinutes <= 0 || string.IsNullOrWhiteSpace(startTime)
            || !Iso8601TimestampParser.TryParse(startTime, out var parsed))
        {
            return RuleResult<NormalizedBookingTime>.Failure(
                BookingRuleErrorCodes.InvalidBookingTime,
                "Start time must be ISO 8601 with an explicit UTC offset and duration must be positive.");
        }

        if (parsed.Ticks % TimeSpan.TicksPerMinute != 0)
        {
            return RuleResult<NormalizedBookingTime>.Failure(
                BookingRuleErrorCodes.InvalidBookingTime,
                "Start time must use whole-minute precision.");
        }

        var startUtc = parsed.UtcDateTime;
        if (startUtc <= authoritativeUtcClock.UtcNow)
        {
            return RuleResult<NormalizedBookingTime>.Failure(
                BookingRuleErrorCodes.InvalidBookingTime,
                "Start time must be strictly later than the authoritative UTC time.");
        }

        var endUtc = startUtc.AddMinutes(durationMinutes);
        var interval = BookingInterval.Create(startUtc, endUtc);
        return RuleResult<NormalizedBookingTime>.Success(new NormalizedBookingTime(interval, durationMinutes));
    }
}
