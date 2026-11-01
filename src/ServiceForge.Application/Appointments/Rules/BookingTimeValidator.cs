using System.Globalization;
using System.Text.RegularExpressions;
using ServiceForge.Application.Abstractions;
using ServiceForge.Domain.Time;

namespace ServiceForge.Application.Appointments.Rules;

public sealed record NormalizedBookingTime(BookingInterval Interval, int DurationMinutes);

public sealed class BookingTimeValidator(IAuthoritativeUtcClock authoritativeUtcClock)
{
    private static readonly Regex ExplicitOffset = new(@"(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant);

    public RuleResult<NormalizedBookingTime> Validate(string? startTime, int durationMinutes)
    {
        if (durationMinutes <= 0 || string.IsNullOrWhiteSpace(startTime) || !ExplicitOffset.IsMatch(startTime))
        {
            return RuleResult<NormalizedBookingTime>.Failure(
                BookingRuleErrorCodes.InvalidBookingTime,
                "Start time must be ISO 8601 with an explicit UTC offset and duration must be positive.");
        }

        if (!DateTimeOffset.TryParse(
                startTime,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var parsed))
        {
            return RuleResult<NormalizedBookingTime>.Failure(
                BookingRuleErrorCodes.InvalidBookingTime,
                "Start time must be a valid ISO 8601 timestamp.");
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
