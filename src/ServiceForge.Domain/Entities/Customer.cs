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

public long Id { get; private set; }
public string Name { get; private set; }
public DateTime CreatedAtUtc { get; private set; }
}
