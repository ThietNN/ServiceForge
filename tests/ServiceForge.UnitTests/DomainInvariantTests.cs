using ServiceForge.Domain.Entities;
using ServiceForge.Domain.Enums;

namespace ServiceForge.UnitTests;

public sealed class DomainInvariantTests
{
    [Fact]
    public void Appointment_requires_both_resources_when_assigned()
    {
        var start = new DateTime(2030, 1, 2, 9, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() => new Appointment(
            1, 1, 2, 3, 4, null, 6, start, start.AddMinutes(30), 30,
            AppointmentStatus.Confirmed, AssignmentStatus.Assigned, null, DateTime.UtcNow));
    }

    [Fact]
    public void Appointment_requires_a_missing_resource_when_needing_reassignment()
    {
        var start = new DateTime(2030, 1, 2, 9, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() => new Appointment(
            1, 1, 2, 3, 4, 5, 6, start, start.AddMinutes(30), 30,
            AppointmentStatus.Confirmed, AssignmentStatus.NeedsReassignment, null, DateTime.UtcNow));
    }
}
