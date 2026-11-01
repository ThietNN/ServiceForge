using FluentValidation;
using ServiceForge.Application.Appointments.Contracts;

namespace ServiceForge.Application.Validation;

public sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator(IValidator<CreateAppointmentRequest> requestValidator)
    {
        RuleFor(command => command.Request).SetValidator(requestValidator);
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(128);
    }
}
