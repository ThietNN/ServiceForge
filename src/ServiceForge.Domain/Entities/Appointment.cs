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
        if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
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

public long Id { get; private set; }
public long CustomerId { get; private set; }
public long VehicleId { get; private set; }
public long DealershipId { get; private set; }
public long ServiceTypeId { get; private set; }
public long? TechnicianId { get; private set; }
public long? ServiceBayId { get; private set; }
public DateTime ScheduledStartUtc { get; private set; }
public DateTime ScheduledEndUtc { get; private set; }
public int DurationMinutes { get; private set; }
public AppointmentStatus Status { get; private set; }
public AssignmentStatus AssignmentStatus { get; private set; }
public DateTime? ActualCompletedAtUtc { get; private set; }
public DateTime CreatedAtUtc { get; private set; }
}
