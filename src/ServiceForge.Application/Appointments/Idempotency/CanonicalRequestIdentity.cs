namespace ServiceForge.Application.Appointments.Idempotency;

public sealed record CanonicalRequestIdentity
{
    public long CustomerId { get; }
    public long VehicleId { get; }
    public long DealershipId { get; }
    public long ServiceTypeId { get; }
    public DateTime StartUtc { get; }

    public CanonicalRequestIdentity(long customerId, long vehicleId, long dealershipId, long serviceTypeId, DateTime startUtc)
    {
        if (customerId <= 0) throw new ArgumentOutOfRangeException(nameof(customerId));
        if (vehicleId <= 0) throw new ArgumentOutOfRangeException(nameof(vehicleId));
        if (dealershipId <= 0) throw new ArgumentOutOfRangeException(nameof(dealershipId));
        if (serviceTypeId <= 0) throw new ArgumentOutOfRangeException(nameof(serviceTypeId));

        CustomerId = customerId;
        VehicleId = vehicleId;
        DealershipId = dealershipId;
        ServiceTypeId = serviceTypeId;
        StartUtc = EnsureUtc(startUtc);
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Canonical request start must be UTC.", nameof(value));
}
