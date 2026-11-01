namespace ServiceForge.Domain.Entities;

public sealed class TechnicianQualification
{
    public TechnicianQualification(long technicianId, long serviceTypeId)
    {
        if (technicianId <= 0) throw new ArgumentOutOfRangeException(nameof(technicianId));
        if (serviceTypeId <= 0) throw new ArgumentOutOfRangeException(nameof(serviceTypeId));

        TechnicianId = technicianId;
        ServiceTypeId = serviceTypeId;
    }

public long TechnicianId { get; private set; }
public long ServiceTypeId { get; private set; }
}
