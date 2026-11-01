namespace ServiceForge.Domain.Entities;

public sealed class ServiceType
{
    public ServiceType(long id, string name, int durationMinutes, DateTime createdAtUtc)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Service type name is required.", nameof(name));
        if (durationMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(durationMinutes));

        Id = id;
        Name = name;
        DurationMinutes = durationMinutes;
        CreatedAtUtc = createdAtUtc;
    }

    public long Id { get; }
    public string Name { get; }
    public int DurationMinutes { get; }
    public DateTime CreatedAtUtc { get; }
}
