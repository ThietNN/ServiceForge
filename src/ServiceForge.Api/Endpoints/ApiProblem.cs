using ServiceForge.Application.Appointments.Contracts;

namespace ServiceForge.Api.Endpoints;

internal static class ApiProblem
{
    public static IResult Write(
        HttpContext context,
        BookingOutcome outcome) =>
        Write(context, outcome.StatusCode, outcome.ErrorCode!, outcome.ErrorMessage!, outcome.ValidationErrors);

    public static IResult Write(
        HttpContext context,
        int statusCode,
        string code,
        string detail,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        if (statusCode is 409 or 503)
        {
            context.Response.Headers.RetryAfter = "2";
        }

        var extensions = new Dictionary<string, object?> { ["code"] = code };
        if (errors is not null)
        {
            extensions["errors"] = errors;
        }

        return Results.Problem(
            statusCode: statusCode,
            title: Title(code),
            detail: detail,
            extensions: extensions);
    }

    private static string Title(string code) => code switch
    {
        "INVALID_REQUEST" => "Invalid request",
        "INVALID_BOOKING_TIME" => "Invalid booking time",
        "OUTSIDE_OPERATING_HOURS" => "Outside operating hours",
        "BOOKING_HORIZON_EXCEEDED" => "Booking horizon exceeded",
        "VEHICLE_NOT_OWNED" => "Vehicle not owned",
        "RESOURCE_NOT_FOUND" => "Resource not found",
        "IDEMPOTENCY_KEY_CONFLICT" => "Idempotency key conflict",
        "REQUEST_IN_PROGRESS" => "Request in progress",
        "BOOKING_TEMPORARILY_UNAVAILABLE" => "Booking temporarily unavailable",
        _ => "Request failed"
    };
}
