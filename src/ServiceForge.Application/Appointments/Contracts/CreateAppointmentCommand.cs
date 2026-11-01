using MediatR;

namespace ServiceForge.Application.Appointments.Contracts;

public sealed record CreateAppointmentCommand(
    CreateAppointmentRequest Request,
    string IdempotencyKey) : IRequest<AppointmentResponse>;
