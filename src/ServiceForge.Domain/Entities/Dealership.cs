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

    public long Id { get; }
    public string Name { get; }
    public string TimeZoneId { get; }
    public DateTime CreatedAtUtc { get; }
}
