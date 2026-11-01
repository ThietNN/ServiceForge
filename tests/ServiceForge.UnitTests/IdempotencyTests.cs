using ServiceForge.Application.Appointments.Idempotency;

namespace ServiceForge.UnitTests;

public sealed class IdempotencyTests
{
    [Fact]
    public void Equivalent_utc_instants_produce_the_same_typed_hash()
    {
        var first = new CanonicalRequestIdentity(1, 2, 3, 4,
            new DateTime(2030, 1, 2, 2, 0, 0, DateTimeKind.Utc));
        var equivalent = new CanonicalRequestIdentity(1, 2, 3, 4,
            new DateTime(2030, 1, 2, 2, 0, 0, DateTimeKind.Utc));

        var firstHash = CanonicalRequestHasher.ComputeHash(first);
        var equivalentHash = CanonicalRequestHasher.ComputeHash(equivalent);

        Assert.Equal(32, firstHash.Length);
        Assert.Equal(firstHash, equivalentHash);
    }

    [Fact]
    public void Any_typed_identity_change_changes_the_hash()
    {
        var original = new CanonicalRequestIdentity(1, 2, 3, 4,
            new DateTime(2030, 1, 2, 2, 0, 0, DateTimeKind.Utc));
        var changed = new CanonicalRequestIdentity(1, 2, 3, 5,
            original.StartUtc);

        Assert.NotEqual(
            CanonicalRequestHasher.ComputeHash(original),
            CanonicalRequestHasher.ComputeHash(changed));
    }

    [Fact]
    public void Equivalent_offset_representations_share_the_same_identity()
    {
        var firstInstant = DateTimeOffset.Parse("2030-01-02T09:00:00+07:00").UtcDateTime;
        var equivalentInstant = DateTimeOffset.Parse("2030-01-02T02:00:00Z").UtcDateTime;

        var first = new CanonicalRequestIdentity(1, 2, 3, 4, firstInstant);
        var equivalent = new CanonicalRequestIdentity(1, 2, 3, 4, equivalentInstant);

        Assert.Equal(
            CanonicalRequestHasher.ComputeHash(first),
            CanonicalRequestHasher.ComputeHash(equivalent));
    }
}
