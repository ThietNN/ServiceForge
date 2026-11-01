namespace ServiceForge.Domain.Entities;

public sealed class ServiceBay
{
    public ServiceBay(long id, long dealershipId, string name, bool isActive, DateTime createdAtUtc)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (dealershipId <= 0) throw new ArgumentOutOfRangeException(nameof(dealershipId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Service bay name is required.", nameof(name));

        Id = id;
        DealershipId = dealershipId;
        Name = name;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public long Id { get; }
    public long DealershipId { get; }
    public string Name { get; }
    public bool IsActive { get; }
    public DateTime CreatedAtUtc { get; }
}
