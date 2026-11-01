using System.Text.RegularExpressions;
using FluentValidation;
using ServiceForge.Application.Appointments.Contracts;

namespace ServiceForge.Application.Validation;

public sealed class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    private static readonly Regex ExplicitOffset = new(@"(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant);

    public CreateAppointmentRequestValidator()
    {
        RuleFor(request => request.CustomerId).GreaterThan(0);
        RuleFor(request => request.VehicleId).GreaterThan(0);
        RuleFor(request => request.DealershipId).GreaterThan(0);
        RuleFor(request => request.ServiceTypeId).GreaterThan(0);
        RuleFor(request => request.StartTime)
            .NotEmpty()
            .Must(HasExplicitWholeMinuteOffset)
            .WithMessage("StartTime must be an ISO 8601 timestamp with an explicit UTC offset and whole-minute precision.");
    }

    private static bool HasExplicitWholeMinuteOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !ExplicitOffset.IsMatch(value))
        {
            return false;
        }

        return Iso8601TimestampParser.TryParse(value, out var parsed)
               && parsed.Ticks % TimeSpan.TicksPerMinute == 0;
    }
}
