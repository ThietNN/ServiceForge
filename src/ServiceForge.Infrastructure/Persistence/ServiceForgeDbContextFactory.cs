using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServiceForge.Infrastructure.Persistence;

public sealed class ServiceForgeDbContextFactory : IDesignTimeDbContextFactory<ServiceForgeDbContext>
{
    public ServiceForgeDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SERVICEFORGE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Set SERVICEFORGE_CONNECTION_STRING before generating or applying EF Core migrations.");
        }

        var options = new DbContextOptionsBuilder<ServiceForgeDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ServiceForgeDbContext(options);
    }
}
