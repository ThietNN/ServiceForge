using Microsoft.EntityFrameworkCore;
using ServiceForge.Domain.Entities;
using ServiceForge.Infrastructure.Persistence.Seed;

namespace ServiceForge.Infrastructure.Persistence;

public sealed class ServiceForgeDbContext(DbContextOptions<ServiceForgeDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Dealership> Dealerships => Set<Dealership>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<ServiceBay> ServiceBays => Set<ServiceBay>();
    public DbSet<TechnicianQualification> TechnicianQualifications => Set<TechnicianQualification>();
    public DbSet<BayServiceCompatibility> BayServiceCompatibilities => Set<BayServiceCompatibility>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServiceForgeDbContext).Assembly);
        ReferenceDataSeed.Apply(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
