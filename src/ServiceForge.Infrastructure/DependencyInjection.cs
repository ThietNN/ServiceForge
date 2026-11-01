using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ServiceForge.Application.Abstractions;
using ServiceForge.Infrastructure.Persistence;
using ServiceForge.Infrastructure.Persistence.Queries;
using ServiceForge.Infrastructure.Persistence.Idempotency;
using ServiceForge.Infrastructure.Persistence.Transactions;
using ServiceForge.Infrastructure.Time;

namespace ServiceForge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceForgeInfrastructure(this IServiceCollection services, string? connectionString = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<ServiceForgeDbContext>(options => options.UseSqlServer(connectionString));
        }
        services.AddScoped<IVehicleBookingQueries, VehicleBookingQueries>();
        services.AddScoped<IBookingResourceQueries, BookingResourceQueries>();
        services.AddScoped<IBookingReferenceDataQueries, BookingReferenceDataQueries>();
        services.AddScoped<IIdempotencyStore, SqlServerIdempotencyStore>();
        services.AddScoped<IAtomicBookingPersistence, SqlServerAtomicBookingPersistence>();
        services.AddScoped<ISqlServerUtcClock, SqlServerUtcClock>();
        services.AddScoped<IBookingLockManager, SqlServerBookingLockManager>();
        services.AddScoped<IBookingTransactionFactory, SqlServerBookingTransactionFactory>();
        services.AddScoped<IAppointmentStore, SqlServerAppointmentStore>();
        services.AddScoped<IReferenceDataReader, SqlServerReferenceDataReader>();
        return services;
    }
}
