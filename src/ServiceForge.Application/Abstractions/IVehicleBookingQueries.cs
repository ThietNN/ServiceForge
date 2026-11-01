using ServiceForge.Domain.Time;

namespace ServiceForge.Application.Abstractions;

public interface IVehicleBookingQueries
{
    Task<bool> IsVehicleOwnedByAsync(long vehicleId, long customerId, CancellationToken cancellationToken);

    Task<bool> HasOccupiedOverlapAsync(
        long vehicleId,
        BookingInterval requestedInterval,
        CancellationToken cancellationToken);
}
