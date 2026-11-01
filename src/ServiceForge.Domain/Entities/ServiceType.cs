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

public long Id { get; private set; }
public string Name { get; private set; }
public int DurationMinutes { get; private set; }
public DateTime CreatedAtUtc { get; private set; }
}
