namespace ServiceForge.Application.Appointments.Rules;

public static class BookingRuleErrorCodes
{
    public const string InvalidBookingTime = "INVALID_BOOKING_TIME";
    public const string OutsideOperatingHours = "OUTSIDE_OPERATING_HOURS";
    public const string BookingHorizonExceeded = "BOOKING_HORIZON_EXCEEDED";
    public const string VehicleNotOwned = "VEHICLE_NOT_OWNED";
    public const string VehicleTimeConflict = "VEHICLE_TIME_CONFLICT";
    public const string NoQualifiedTechnician = "NO_QUALIFIED_TECHNICIAN";
    public const string NoCompatibleBay = "NO_COMPATIBLE_BAY";
    public const string NoAvailableResources = "NO_AVAILABLE_RESOURCES";
}

public sealed record RuleResult<T>(bool IsValid, T? Value, string? ErrorCode, string? ErrorMessage)
{
    public static RuleResult<T> Success(T value) => new(true, value, null, null);

    public static RuleResult<T> Failure(string errorCode, string errorMessage) =>
        new(false, default, errorCode, errorMessage);
}
