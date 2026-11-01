using ServiceForge.Domain.Entities;

namespace ServiceForge.Application.Appointments.Idempotency;

public enum IdempotencyLookupStatus
{
    Missing,
    MatchingUnexpired,
    ConflictingUnexpired,
    Expired
}

public sealed record IdempotencyLookupResult(
    IdempotencyLookupStatus Status,
    IdempotencyRecord? Record)
{
    public string? Location => Record is null ? null : $"/api/appointments/{Record.AppointmentId}";
}

public sealed record IdempotencyRecordData(
    string Key,
    byte[] RequestHash,
    short ResponseStatusCode,
    string ResponseBody,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc);
