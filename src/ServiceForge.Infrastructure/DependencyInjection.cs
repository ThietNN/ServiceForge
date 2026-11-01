using Microsoft.Extensions.DependencyInjection;
using ServiceForge.Application.Abstractions;
using ServiceForge.Infrastructure.Persistence.Queries;

namespace ServiceForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceForgeInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IVehicleBookingQueries, VehicleBookingQueries>();
        services.AddScoped<IBookingResourceQueries, BookingResourceQueries>();
        return services;
    }
}
