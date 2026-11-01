using ServiceForge.Application.Abstractions;
using ServiceForge.Domain.Time;

namespace ServiceForge.Application.Appointments.Rules;

public sealed record DealershipLocalBookingWindow(DateTime StartLocal, DateTime EndLocal);

public sealed class DealershipLocalTimeValidator(IAuthoritativeUtcClock authoritativeUtcClock)
{
    public RuleResult<DealershipLocalBookingWindow> Validate(BookingInterval interval, TimeZoneInfo dealershipTimeZone)
    {
        ArgumentNullException.ThrowIfNull(dealershipTimeZone);

        var startLocal = TimeZoneInfo.ConvertTimeFromUtc(interval.StartUtc, dealershipTimeZone);
        var endLocal = TimeZoneInfo.ConvertTimeFromUtc(interval.EndUtc, dealershipTimeZone);

        if (startLocal.Date != endLocal.Date || startLocal.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            || endLocal.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            || startLocal.TimeOfDay < TimeSpan.FromHours(9)
            || endLocal.TimeOfDay > TimeSpan.FromHours(17))
        {
            return RuleResult<DealershipLocalBookingWindow>.Failure(
                BookingRuleErrorCodes.OutsideOperatingHours,
                "The entire appointment must fit within a weekday operating window from 09:00 through 17:00 local time.");
        }

        var nowLocalDate = TimeZoneInfo.ConvertTimeFromUtc(authoritativeUtcClock.UtcNow, dealershipTimeZone).Date;
        var horizonDate = nowLocalDate.AddYears(1);
        if (startLocal.Date > horizonDate)
        {
            return RuleResult<DealershipLocalBookingWindow>.Failure(
                BookingRuleErrorCodes.BookingHorizonExceeded,
                "The appointment start is beyond the inclusive one-local-calendar-year booking horizon.");
        }

        return RuleResult<DealershipLocalBookingWindow>.Success(
            new DealershipLocalBookingWindow(startLocal, endLocal));
    }
}
