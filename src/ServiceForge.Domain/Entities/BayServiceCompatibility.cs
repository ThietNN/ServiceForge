namespace ServiceForge.Domain.Entities;

public sealed class BayServiceCompatibility
{
    public BayServiceCompatibility(long serviceBayId, long serviceTypeId)
    {
        if (serviceBayId <= 0) throw new ArgumentOutOfRangeException(nameof(serviceBayId));
        if (serviceTypeId <= 0) throw new ArgumentOutOfRangeException(nameof(serviceTypeId));

        ServiceBayId = serviceBayId;
        ServiceTypeId = serviceTypeId;
    }

public long ServiceBayId { get; private set; }
public long ServiceTypeId { get; private set; }
}
