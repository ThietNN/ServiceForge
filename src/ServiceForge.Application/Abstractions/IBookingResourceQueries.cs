using ServiceForge.Application.Appointments.Rules;
using ServiceForge.Domain.Time;

namespace ServiceForge.Application.Abstractions;

public interface IBookingResourceQueries
{
    Task<ResourceEligibilityResult> GetEligibilityAsync(
        long dealershipId,
        long serviceTypeId,
        BookingInterval requestedInterval,
        CancellationToken cancellationToken);
}
