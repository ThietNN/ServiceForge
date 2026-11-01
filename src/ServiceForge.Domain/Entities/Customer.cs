namespace ServiceForge.Domain.Entities;

public sealed class Customer
{
    public Customer(long id, string name, DateTime createdAtUtc)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Customer name is required.", nameof(name));

        Id = id;
        Name = name;
        CreatedAtUtc = createdAtUtc;
    }

    public long Id { get; }
    public string Name { get; }
    public DateTime CreatedAtUtc { get; }
}
