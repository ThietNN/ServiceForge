using System.Buffers.Binary;
using System.Security.Cryptography;

namespace ServiceForge.Application.Appointments.Idempotency;

public static class CanonicalRequestHasher
{
    public static byte[] ComputeHash(CanonicalRequestIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        Span<byte> canonical = stackalloc byte[1 + (sizeof(long) * 5)];
        canonical[0] = 1;
        BinaryPrimitives.WriteInt64LittleEndian(canonical[1..9], identity.CustomerId);
        BinaryPrimitives.WriteInt64LittleEndian(canonical[9..17], identity.VehicleId);
        BinaryPrimitives.WriteInt64LittleEndian(canonical[17..25], identity.DealershipId);
        BinaryPrimitives.WriteInt64LittleEndian(canonical[25..33], identity.ServiceTypeId);
        BinaryPrimitives.WriteInt64LittleEndian(canonical[33..41], identity.StartUtc.Ticks);

        return SHA256.HashData(canonical);
    }
}
