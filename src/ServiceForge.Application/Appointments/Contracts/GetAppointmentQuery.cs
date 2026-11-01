using MediatR;

namespace ServiceForge.Application.Appointments.Contracts;

public sealed record GetAppointmentQuery(long AppointmentId) : IRequest<AppointmentResponse?>;
