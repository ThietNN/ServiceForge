namespace ServiceForge.Application.Appointments.Contracts;

public sealed record CreateAppointmentRequest(
    long CustomerId,
    long VehicleId,
    long DealershipId,
    long ServiceTypeId,
    string StartTime);
