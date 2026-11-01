using MediatR;
using ServiceForge.Application.Abstractions;
using ServiceForge.Application.Appointments.Contracts;

namespace ServiceForge.Application.Appointments.Handlers;

public sealed class GetAppointmentQueryHandler(IAppointmentStore appointmentStore)
    : IRequestHandler<GetAppointmentQuery, AppointmentResponse?>
{
    public Task<AppointmentResponse?> Handle(GetAppointmentQuery request, CancellationToken cancellationToken) =>
        appointmentStore.GetByIdAsync(request.AppointmentId, cancellationToken);
}
