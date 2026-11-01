using ServiceForge.Domain.Enums;

namespace ServiceForge.Domain.Entities;

public sealed class Appointment
{
    public Appointment(
        long id,
        long customerId,
        long vehicleId,
        long dealershipId,
        long serviceTypeId,
        long? technicianId,
        long? serviceBayId,
        DateTime scheduledStartUtc,
        DateTime scheduledEndUtc,
        int durationMinutes,
        AppointmentStatus status,
        AssignmentStatus assignmentStatus,
        DateTime? actualCompletedAtUtc,
        DateTime createdAtUtc)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (customerId <= 0) throw new ArgumentOutOfRangeException(nameof(customerId));
        if (vehicleId <= 0) throw new ArgumentOutOfRangeException(nameof(vehicleId));
        if (dealershipId <= 0) throw new ArgumentOutOfRangeException(nameof(dealershipId));
        if (serviceTypeId <= 0) throw new ArgumentOutOfRangeException(nameof(serviceTypeId));
        if (scheduledEndUtc <= scheduledStartUtc) throw new ArgumentException("Appointment end must be after its start.", nameof(scheduledEndUtc));
        if (scheduledStartUtc.Ticks % TimeSpan.TicksPerMinute != 0) throw new ArgumentException("Appointment start must be whole-minute aligned.", nameof(scheduledStartUtc));
        if (durationMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(durationMinutes));
        if (assignmentStatus == AssignmentStatus.Assigned && (technicianId is null || serviceBayId is null))
            throw new ArgumentException("Assigned appointments require a technician and service bay.", nameof(assignmentStatus));
        if (assignmentStatus == AssignmentStatus.NeedsReassignment && technicianId is not null && serviceBayId is not null)
            throw new ArgumentException("NeedsReassignment requires at least one missing resource.", nameof(assignmentStatus));
        if ((status == AppointmentStatus.Completed) != actualCompletedAtUtc.HasValue)
            throw new ArgumentException("Completed status and completion timestamp must agree.", nameof(actualCompletedAtUtc));

        Id = id;
        CustomerId = customerId;
        VehicleId = vehicleId;
        DealershipId = dealershipId;
        ServiceTypeId = serviceTypeId;
        TechnicianId = technicianId;
        ServiceBayId = serviceBayId;
        ScheduledStartUtc = scheduledStartUtc;
        ScheduledEndUtc = scheduledEndUtc;
        DurationMinutes = durationMinutes;
        Status = status;
        AssignmentStatus = assignmentStatus;
        ActualCompletedAtUtc = actualCompletedAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public long Id { get; }
    public long CustomerId { get; }
    public long VehicleId { get; }
    public long DealershipId { get; }
    public long ServiceTypeId { get; }
    public long? TechnicianId { get; }
    public long? ServiceBayId { get; }
    public DateTime ScheduledStartUtc { get; }
    public DateTime ScheduledEndUtc { get; }
    public int DurationMinutes { get; }
    public AppointmentStatus Status { get; }
    public AssignmentStatus AssignmentStatus { get; }
    public DateTime? ActualCompletedAtUtc { get; }
    public DateTime CreatedAtUtc { get; }
}
