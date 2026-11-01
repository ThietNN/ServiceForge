using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceForge.Domain.Entities;
using ServiceForge.Domain.Enums;

namespace ServiceForge.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customer");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.ToTable("Vehicle");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.RegistrationNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasAlternateKey(x => new { x.Id, x.CustomerId });
        builder.HasIndex(x => new { x.CustomerId, x.Id });
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class DealershipConfiguration : IEntityTypeConfiguration<Dealership>
{
    public void Configure(EntityTypeBuilder<Dealership> builder)
    {
        builder.ToTable("Dealership");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

public sealed class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.ToTable("ServiceType", table => table.HasCheckConstraint("CK_ServiceType_DurationMinutes_Positive", "[DurationMinutes] > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DurationMinutes).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

public sealed class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    public void Configure(EntityTypeBuilder<Technician> builder)
    {
        builder.ToTable("Technician");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasAlternateKey(x => new { x.Id, x.DealershipId });
        builder.HasIndex(x => new { x.DealershipId, x.IsActive, x.Id });
        builder.HasOne<Dealership>().WithMany().HasForeignKey(x => x.DealershipId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class ServiceBayConfiguration : IEntityTypeConfiguration<ServiceBay>
{
    public void Configure(EntityTypeBuilder<ServiceBay> builder)
    {
        builder.ToTable("ServiceBay");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasAlternateKey(x => new { x.Id, x.DealershipId });
        builder.HasIndex(x => new { x.DealershipId, x.IsActive, x.Id });
        builder.HasOne<Dealership>().WithMany().HasForeignKey(x => x.DealershipId).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class TechnicianQualificationConfiguration : IEntityTypeConfiguration<TechnicianQualification>
{
    public void Configure(EntityTypeBuilder<TechnicianQualification> builder)
    {
        builder.ToTable("TechnicianQualification");
        builder.HasKey(x => new { x.TechnicianId, x.ServiceTypeId });
        builder.HasOne<Technician>().WithMany().HasForeignKey(x => x.TechnicianId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ServiceType>().WithMany().HasForeignKey(x => x.ServiceTypeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => new { x.ServiceTypeId, x.TechnicianId });
    }
}

public sealed class BayServiceCompatibilityConfiguration : IEntityTypeConfiguration<BayServiceCompatibility>
{
    public void Configure(EntityTypeBuilder<BayServiceCompatibility> builder)
    {
        builder.ToTable("BayServiceCompatibility");
        builder.HasKey(x => new { x.ServiceBayId, x.ServiceTypeId });
        builder.HasOne<ServiceBay>().WithMany().HasForeignKey(x => x.ServiceBayId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ServiceType>().WithMany().HasForeignKey(x => x.ServiceTypeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => new { x.ServiceTypeId, x.ServiceBayId });
    }
}

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointment", table =>
        {
            table.HasCheckConstraint("CK_Appointment_EndAfterStart", "[ScheduledEndUtc] > [ScheduledStartUtc]");
            table.HasCheckConstraint("CK_Appointment_DurationMinutes_Positive", "[DurationMinutes] > 0");
            table.HasCheckConstraint("CK_Appointment_Start_WholeMinute", "DATEPART(second, [ScheduledStartUtc]) = 0 AND DATEPART(nanosecond, [ScheduledStartUtc]) = 0");
            table.HasCheckConstraint("CK_Appointment_Status_Allowed", "[Status] IN ('Confirmed', 'Cancelled', 'Completed')");
            table.HasCheckConstraint("CK_Appointment_AssignmentStatus_Allowed", "[AssignmentStatus] IN ('Assigned', 'NeedsReassignment')");
            table.HasCheckConstraint("CK_Appointment_AssignedResources", "[AssignmentStatus] <> 'Assigned' OR ([TechnicianId] IS NOT NULL AND [ServiceBayId] IS NOT NULL)");
            table.HasCheckConstraint("CK_Appointment_NeedsReassignmentResource", "[AssignmentStatus] <> 'NeedsReassignment' OR [TechnicianId] IS NULL OR [ServiceBayId] IS NULL");
            table.HasCheckConstraint("CK_Appointment_CompletionConsistency", "([Status] = 'Completed' AND [ActualCompletedAtUtc] IS NOT NULL) OR ([Status] <> 'Completed' AND [ActualCompletedAtUtc] IS NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ScheduledStartUtc).HasColumnType("datetime2(7)").IsRequired();
        builder.Property(x => x.ScheduledEndUtc).HasColumnType("datetime2(7)").IsRequired();
        builder.Property(x => x.ActualCompletedAtUtc).HasColumnType("datetime2(7)");
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.Status).HasConversion<string>().HasColumnType("varchar(32)").HasMaxLength(32).IsRequired();
        builder.Property(x => x.AssignmentStatus).HasConversion<string>().HasColumnType("varchar(32)").HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.VehicleId, x.ScheduledStartUtc });
        builder.HasIndex(x => new { x.TechnicianId, x.ScheduledStartUtc });
        builder.HasIndex(x => new { x.ServiceBayId, x.ScheduledStartUtc });
        builder.HasIndex(x => new { x.DealershipId, x.ScheduledStartUtc });
        builder.HasIndex(x => new { x.CustomerId, x.ScheduledStartUtc });
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Vehicle>().WithMany().HasPrincipalKey(x => new { x.Id, x.CustomerId }).HasForeignKey(x => new { x.VehicleId, x.CustomerId }).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Dealership>().WithMany().HasForeignKey(x => x.DealershipId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ServiceType>().WithMany().HasForeignKey(x => x.ServiceTypeId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Technician>().WithMany().HasPrincipalKey(x => new { x.Id, x.DealershipId }).HasForeignKey(x => new { x.TechnicianId, x.DealershipId }).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<ServiceBay>().WithMany().HasPrincipalKey(x => new { x.Id, x.DealershipId }).HasForeignKey(x => new { x.ServiceBayId, x.DealershipId }).OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecord", table => table.HasCheckConstraint("CK_IdempotencyRecord_ExpiryAfterCreation", "[ExpiresAtUtc] > [CreatedAtUtc]"));
        builder.HasKey(x => x.Key);
        builder.Property(x => x.Key).HasColumnType("varchar(128)").HasMaxLength(128).IsRequired();
        builder.Property(x => x.RequestHash).HasColumnType("binary(32)").IsRequired();
        builder.Property(x => x.ResponseStatusCode).HasColumnType("smallint").IsRequired();
        builder.Property(x => x.ResponseBody).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(x => x.ExpiresAtUtc).HasColumnType("datetime2(7)").IsRequired();
        builder.HasIndex(x => x.AppointmentId).IsUnique().HasFilter("[AppointmentId] IS NOT NULL");
        builder.HasIndex(x => x.ExpiresAtUtc);
        builder.HasOne<Appointment>().WithOne().HasForeignKey<IdempotencyRecord>(x => x.AppointmentId).OnDelete(DeleteBehavior.NoAction);
    }
}
