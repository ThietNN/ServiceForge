using Microsoft.EntityFrameworkCore;
using ServiceForge.Application.Abstractions;
using ServiceForge.Application.Appointments.Rules;
using ServiceForge.Domain.Enums;
using ServiceForge.Domain.Time;

namespace ServiceForge.Infrastructure.Persistence.Queries;

public sealed class VehicleBookingQueries(ServiceForgeDbContext dbContext) : IVehicleBookingQueries
{
    public Task<bool> IsVehicleOwnedByAsync(long vehicleId, long customerId, CancellationToken cancellationToken) =>
        dbContext.Vehicles.AsNoTracking().AnyAsync(
            vehicle => vehicle.Id == vehicleId && vehicle.CustomerId == customerId,
            cancellationToken);

    public Task<bool> HasOccupiedOverlapAsync(
        long vehicleId,
        BookingInterval requestedInterval,
        CancellationToken cancellationToken) =>
        dbContext.Appointments.AsNoTracking().AnyAsync(
            appointment => appointment.VehicleId == vehicleId
                && appointment.Status != AppointmentStatus.Cancelled
                && appointment.ScheduledStartUtc < requestedInterval.EndUtc
                && appointment.ScheduledEndUtc > requestedInterval.StartUtc,
            cancellationToken);
}

public sealed class BookingResourceQueries(ServiceForgeDbContext dbContext) : IBookingResourceQueries
{
    public async Task<ResourceEligibilityResult> GetEligibilityAsync(
        long dealershipId,
        long serviceTypeId,
        BookingInterval requestedInterval,
        CancellationToken cancellationToken)
    {
        var hasQualifiedTechnician = await dbContext.Technicians.AsNoTracking().AnyAsync(
            technician => technician.DealershipId == dealershipId
                && technician.IsActive
                && dbContext.TechnicianQualifications.Any(qualification =>
                    qualification.TechnicianId == technician.Id
                    && qualification.ServiceTypeId == serviceTypeId),
            cancellationToken);

        var hasCompatibleBay = await dbContext.ServiceBays.AsNoTracking().AnyAsync(
            bay => bay.DealershipId == dealershipId
                && bay.IsActive
                && dbContext.BayServiceCompatibilities.Any(compatibility =>
                    compatibility.ServiceBayId == bay.Id
                    && compatibility.ServiceTypeId == serviceTypeId),
            cancellationToken);

        var availableTechnicianIds = await dbContext.Technicians.AsNoTracking()
            .Where(technician => technician.DealershipId == dealershipId
                && technician.IsActive
                && dbContext.TechnicianQualifications.Any(qualification =>
                    qualification.TechnicianId == technician.Id
                    && qualification.ServiceTypeId == serviceTypeId)
                && !dbContext.Appointments.Any(appointment =>
                    appointment.TechnicianId == technician.Id
                    && appointment.Status != AppointmentStatus.Cancelled
                    && appointment.ScheduledStartUtc < requestedInterval.EndUtc
                    && appointment.ScheduledEndUtc > requestedInterval.StartUtc))
            .OrderBy(technician => technician.Id)
            .Select(technician => technician.Id)
            .ToListAsync(cancellationToken);

        var availableBayIds = await dbContext.ServiceBays.AsNoTracking()
            .Where(bay => bay.DealershipId == dealershipId
                && bay.IsActive
                && dbContext.BayServiceCompatibilities.Any(compatibility =>
                    compatibility.ServiceBayId == bay.Id
                    && compatibility.ServiceTypeId == serviceTypeId)
                && !dbContext.Appointments.Any(appointment =>
                    appointment.ServiceBayId == bay.Id
                    && appointment.Status != AppointmentStatus.Cancelled
                    && appointment.ScheduledStartUtc < requestedInterval.EndUtc
                    && appointment.ScheduledEndUtc > requestedInterval.StartUtc))
            .OrderBy(bay => bay.Id)
            .Select(bay => bay.Id)
            .ToListAsync(cancellationToken);

        return new ResourceEligibilityResult(
            hasQualifiedTechnician,
            hasCompatibleBay,
            availableTechnicianIds,
            availableBayIds);
    }
}
