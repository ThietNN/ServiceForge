namespace ServiceForge.Domain.Entities;

public sealed class Vehicle
{
    public Vehicle(long id, long customerId, string registrationNumber, DateTime createdAtUtc)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (customerId <= 0) throw new ArgumentOutOfRangeException(nameof(customerId));
        if (string.IsNullOrWhiteSpace(registrationNumber)) throw new ArgumentException("Registration number is required.", nameof(registrationNumber));

        Id = id;
        CustomerId = customerId;
        RegistrationNumber = registrationNumber;
        CreatedAtUtc = createdAtUtc;
    }

    public long Id { get; }
    public long CustomerId { get; }
    public string RegistrationNumber { get; }
    public DateTime CreatedAtUtc { get; }
}
