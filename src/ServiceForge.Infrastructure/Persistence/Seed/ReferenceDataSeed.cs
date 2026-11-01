using Microsoft.EntityFrameworkCore;

namespace ServiceForge.Infrastructure.Persistence.Seed;

internal static class ReferenceDataSeed
{
    private static readonly DateTime SeededAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServiceForge.Domain.Entities.Customer>().HasData(
            new { Id = 1L, Name = "Demo Customer One", CreatedAtUtc = SeededAtUtc },
            new { Id = 2L, Name = "Demo Customer Two", CreatedAtUtc = SeededAtUtc });

        modelBuilder.Entity<ServiceForge.Domain.Entities.Vehicle>().HasData(
            new { Id = 1L, CustomerId = 1L, RegistrationNumber = "SF-1001", CreatedAtUtc = SeededAtUtc },
            new { Id = 2L, CustomerId = 2L, RegistrationNumber = "SF-2001", CreatedAtUtc = SeededAtUtc });

        modelBuilder.Entity<ServiceForge.Domain.Entities.Dealership>().HasData(
            new { Id = 1L, Name = "Bangkok ServiceForge", TimeZoneId = "Asia/Bangkok", CreatedAtUtc = SeededAtUtc },
            new { Id = 2L, Name = "London ServiceForge", TimeZoneId = "Europe/London", CreatedAtUtc = SeededAtUtc },
            new { Id = 3L, Name = "New York ServiceForge", TimeZoneId = "America/New_York", CreatedAtUtc = SeededAtUtc });

        modelBuilder.Entity<ServiceForge.Domain.Entities.ServiceType>().HasData(
            new { Id = 1L, Name = "Oil Service", DurationMinutes = 60, CreatedAtUtc = SeededAtUtc },
            new { Id = 2L, Name = "Brake Inspection", DurationMinutes = 90, CreatedAtUtc = SeededAtUtc },
            new { Id = 3L, Name = "Annual Service", DurationMinutes = 120, CreatedAtUtc = SeededAtUtc });

        modelBuilder.Entity<ServiceForge.Domain.Entities.Technician>().HasData(
            new { Id = 1L, DealershipId = 1L, Name = "Bangkok Technician One", IsActive = true, CreatedAtUtc = SeededAtUtc },
            new { Id = 2L, DealershipId = 1L, Name = "Bangkok Technician Two", IsActive = true, CreatedAtUtc = SeededAtUtc },
            new { Id = 3L, DealershipId = 2L, Name = "London Technician One", IsActive = true, CreatedAtUtc = SeededAtUtc },
            new { Id = 4L, DealershipId = 3L, Name = "New York Technician One", IsActive = true, CreatedAtUtc = SeededAtUtc });

        modelBuilder.Entity<ServiceForge.Domain.Entities.ServiceBay>().HasData(
            new { Id = 1L, DealershipId = 1L, Name = "Bangkok Bay One", IsActive = true, CreatedAtUtc = SeededAtUtc },
            new { Id = 2L, DealershipId = 1L, Name = "Bangkok Bay Two", IsActive = true, CreatedAtUtc = SeededAtUtc },
            new { Id = 3L, DealershipId = 2L, Name = "London Bay One", IsActive = true, CreatedAtUtc = SeededAtUtc },
            new { Id = 4L, DealershipId = 3L, Name = "New York Bay One", IsActive = true, CreatedAtUtc = SeededAtUtc });

        modelBuilder.Entity<ServiceForge.Domain.Entities.TechnicianQualification>().HasData(
            new { TechnicianId = 1L, ServiceTypeId = 1L },
            new { TechnicianId = 1L, ServiceTypeId = 2L },
            new { TechnicianId = 2L, ServiceTypeId = 1L },
            new { TechnicianId = 3L, ServiceTypeId = 2L },
            new { TechnicianId = 4L, ServiceTypeId = 3L });

        modelBuilder.Entity<ServiceForge.Domain.Entities.BayServiceCompatibility>().HasData(
            new { ServiceBayId = 1L, ServiceTypeId = 1L },
            new { ServiceBayId = 1L, ServiceTypeId = 2L },
            new { ServiceBayId = 2L, ServiceTypeId = 1L },
            new { ServiceBayId = 3L, ServiceTypeId = 2L },
            new { ServiceBayId = 4L, ServiceTypeId = 3L });
    }
}
