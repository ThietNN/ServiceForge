using ServiceForge.Application.Appointments.Rules;

namespace ServiceForge.Application.Appointments.Allocation;

public sealed record ResourceAllocation(long TechnicianId, long ServiceBayId);

public static class ResourceAllocator
{
    public static RuleResult<ResourceAllocation> SelectLowestAvailable(ResourceEligibilityResult eligibility)
    {
        ArgumentNullException.ThrowIfNull(eligibility);

        var errorCode = ResourceEligibilityRules.GetErrorCode(eligibility);
        if (errorCode is not null)
        {
            return RuleResult<ResourceAllocation>.Failure(errorCode, errorCode);
        }

        return RuleResult<ResourceAllocation>.Success(new ResourceAllocation(
            eligibility.AvailableTechnicianIds.Min(),
            eligibility.AvailableBayIds.Min()));
    }
}
