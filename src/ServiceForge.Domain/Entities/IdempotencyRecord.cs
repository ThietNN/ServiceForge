namespace ServiceForge.Domain.Entities;

public sealed class IdempotencyRecord
{
    public IdempotencyRecord(
        string key,
        byte[] requestHash,
        long appointmentId,
        short responseStatusCode,
        string responseBody,
        DateTime createdAtUtc,
        DateTime expiresAtUtc)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128) throw new ArgumentException("Idempotency key must be 1 to 128 characters.", nameof(key));
        if (requestHash is null || requestHash.Length != 32) throw new ArgumentException("Request hash must be 32 bytes.", nameof(requestHash));
        if (appointmentId <= 0) throw new ArgumentOutOfRangeException(nameof(appointmentId));
        if (responseStatusCode <= 0) throw new ArgumentOutOfRangeException(nameof(responseStatusCode));
        if (responseBody is null) throw new ArgumentNullException(nameof(responseBody));
        if (expiresAtUtc <= createdAtUtc) throw new ArgumentException("Expiration must be after creation.", nameof(expiresAtUtc));

        Key = key;
        RequestHash = requestHash.ToArray();
        AppointmentId = appointmentId;
        ResponseStatusCode = responseStatusCode;
        ResponseBody = responseBody;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public string Key { get; }
    public byte[] RequestHash { get; }
    public long AppointmentId { get; }
    public short ResponseStatusCode { get; }
    public string ResponseBody { get; }
    public DateTime CreatedAtUtc { get; }
    public DateTime ExpiresAtUtc { get; }
}
