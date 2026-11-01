namespace ServiceForge.Domain.Time;

public sealed record BookingInterval
{
    private BookingInterval(DateTime startUtc, DateTime endUtc)
    {
        StartUtc = startUtc;
        EndUtc = endUtc;
    }

    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }

    public static BookingInterval Create(DateTime startUtc, DateTime endUtc)
    {
        if (startUtc.Kind != DateTimeKind.Utc || endUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Booking interval endpoints must be UTC.");
        }

        if (endUtc <= startUtc)
        {
            throw new ArgumentException("Booking interval end must be after its start.");
        }

        return new BookingInterval(startUtc, endUtc);
    }

    public bool Overlaps(BookingInterval other) =>
        StartUtc < other.EndUtc && EndUtc > other.StartUtc;
}
