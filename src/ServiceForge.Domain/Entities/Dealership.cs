namespace ServiceForge.Domain.Entities;

public sealed class Dealership
{
    public Dealership(long id, string name, string timeZoneId, DateTime createdAtUtc)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Dealership name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(timeZoneId)) throw new ArgumentException("Timezone ID is required.", nameof(timeZoneId));

        Id = id;
        Name = name;
        TimeZoneId = timeZoneId;
        CreatedAtUtc = createdAtUtc;
    }

public long Id { get; private set; }
public string Name { get; private set; }
public string TimeZoneId { get; private set; }
public DateTime CreatedAtUtc { get; private set; }
}
