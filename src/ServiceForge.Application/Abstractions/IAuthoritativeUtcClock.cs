namespace ServiceForge.Application.Abstractions;

public interface IAuthoritativeUtcClock
{
    DateTime UtcNow { get; }
}
