namespace ServiceForge.Application.Appointments.Rules;

public sealed record ResourceEligibilityResult(
    bool HasQualifiedTechnician,
    bool HasCompatibleBay,
    IReadOnlyList<long> AvailableTechnicianIds,
    IReadOnlyList<long> AvailableBayIds);

public static class ResourceEligibilityRules
{
    public static string? GetErrorCode(ResourceEligibilityResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.HasQualifiedTechnician)
        {
            return BookingRuleErrorCodes.NoQualifiedTechnician;
        }

        if (!result.HasCompatibleBay)
        {
            return BookingRuleErrorCodes.NoCompatibleBay;
        }

        if (result.AvailableTechnicianIds.Count == 0 || result.AvailableBayIds.Count == 0)
        {
            return BookingRuleErrorCodes.NoAvailableResources;
        }

        return null;
    }
}
