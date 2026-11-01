using ServiceForge.Application.Appointments.Contracts;
using ServiceForge.Domain.Entities;

namespace ServiceForge.Application.Abstractions;

public interface IAppointmentStore
{
    Task<AppointmentResponse?> GetByIdAsync(long appointmentId, CancellationToken cancellationToken);

    Task AddAsync(Appointment appointment, CancellationToken cancellationToken);
}
