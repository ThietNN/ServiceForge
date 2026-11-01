using Microsoft.EntityFrameworkCore;
using ServiceForge.Application.Abstractions;

namespace ServiceForge.Infrastructure.Persistence.Queries;

public sealed class BookingReferenceDataQueries(ServiceForgeDbContext dbContext) : IBookingReferenceDataQueries
{
    public async Task<BookingReferenceData> GetAsync(
        long customerId,
        long vehicleId,
        long dealershipId,
        long serviceTypeId,
        CancellationToken cancellationToken)
    {
        var customerExists = await dbContext.Customers.AsNoTracking()
            .AnyAsync(item => item.Id == customerId, cancellationToken);
        var vehicle = await dbContext.Vehicles.AsNoTracking()
            .Where(item => item.Id == vehicleId)
            .Select(item => new { item.CustomerId })
            .SingleOrDefaultAsync(cancellationToken);
        var dealership = await dbContext.Dealerships.AsNoTracking()
            .Where(item => item.Id == dealershipId)
            .Select(item => new { item.TimeZoneId })
            .SingleOrDefaultAsync(cancellationToken);
        var serviceType = await dbContext.ServiceTypes.AsNoTracking()
            .Where(item => item.Id == serviceTypeId)
            .Select(item => new { item.DurationMinutes })
            .SingleOrDefaultAsync(cancellationToken);

        return new BookingReferenceData(
            customerExists,
            vehicle is not null,
            vehicle?.CustomerId,
            dealership is not null,
            dealership?.TimeZoneId,
            serviceType is not null,
            serviceType?.DurationMinutes);
    }
}
