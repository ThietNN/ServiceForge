using Microsoft.Extensions.DependencyInjection;

namespace ServiceForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceForgeInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
