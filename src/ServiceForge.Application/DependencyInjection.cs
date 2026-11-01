using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceForge.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceForgeApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
