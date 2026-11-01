namespace ServiceForge.Application.Abstractions;

public sealed record BookingReferenceData(
    bool CustomerExists,
    bool VehicleExists,
    long? VehicleOwnerCustomerId,
    bool DealershipExists,
    string? DealershipTimeZoneId,
    bool ServiceTypeExists,
    int? ServiceDurationMinutes);

public interface IBookingReferenceDataQueries
{
    Task<BookingReferenceData> GetAsync(
        long customerId,
        long vehicleId,
        long dealershipId,
        long serviceTypeId,
        CancellationToken cancellationToken);
}
