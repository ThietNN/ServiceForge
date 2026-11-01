namespace ServiceForge.Application.Appointments.Contracts;

public sealed record BookingOutcome(
    int StatusCode,
    string? ErrorCode,
    string? ErrorMessage,
    AppointmentResponse? Response,
    string? Location,
    bool IsReplay = false,
    IReadOnlyDictionary<string, string[]>? ValidationErrors = null)
{
    public bool IsSuccess => Response is not null && ErrorCode is null;

    public static BookingOutcome Success(AppointmentResponse response, string location, bool isReplay = false) =>
        new(201, null, null, response, location, isReplay);

    public static BookingOutcome Failure(int statusCode, string code, string message) =>
        new(statusCode, code, message, null, null);

    public static BookingOutcome Invalid(IReadOnlyDictionary<string, string[]> errors) =>
        new(400, "INVALID_REQUEST", "The request is invalid.", null, null, false, errors);

    public static BookingOutcome InvalidBookingTime(IReadOnlyDictionary<string, string[]> errors) =>
        new(400, "INVALID_BOOKING_TIME", "The booking time is invalid.", null, null, false, errors);
}
