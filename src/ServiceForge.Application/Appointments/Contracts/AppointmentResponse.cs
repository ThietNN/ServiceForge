using ServiceForge.Domain.Enums;

namespace ServiceForge.Application.Appointments.Contracts;

public sealed record AppointmentResponse(
    long Id,
    AppointmentStatus Status,
    AssignmentStatus AssignmentStatus,
    long CustomerId,
    long VehicleId,
    long DealershipId,
    long ServiceTypeId,
    long? TechnicianId,
    long? ServiceBayId,
    DateTime ScheduledStartUtc,
    DateTime ScheduledEndUtc,
    int DurationMinutes);
