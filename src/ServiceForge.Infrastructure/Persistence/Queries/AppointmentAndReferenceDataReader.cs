using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ServiceForge.Application.Abstractions;
using ServiceForge.Application.Appointments.Contracts;
using ServiceForge.Application.Common.Pagination;
using ServiceForge.Application.ReferenceData.Contracts;
using ServiceForge.Domain.Entities;

namespace ServiceForge.Infrastructure.Persistence.Queries;

public sealed class SqlServerAppointmentStore(ServiceForgeDbContext dbContext) : IAppointmentStore
{
    public async Task<AppointmentResponse?> GetByIdAsync(long appointmentId, CancellationToken cancellationToken)
    {
        return await dbContext.Appointments.AsNoTracking()
            .Where(item => item.Id == appointmentId)
            .Select(item => new AppointmentResponse(
                item.Id, item.Status, item.AssignmentStatus, item.CustomerId, item.VehicleId,
                item.DealershipId, item.ServiceTypeId, item.TechnicianId, item.ServiceBayId,
                item.ScheduledStartUtc, item.ScheduledEndUtc, item.DurationMinutes))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken)
    {
        dbContext.Appointments.Add(appointment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

public sealed class SqlServerReferenceDataReader(ServiceForgeDbContext dbContext) : IReferenceDataReader
{
    public Task<PagedResponse<VehicleResponse>> GetCustomerVehiclesAsync(
        long customerId, PaginationRequest pagination, CancellationToken cancellationToken) =>
        PageAsync(
            dbContext.Vehicles.AsNoTracking().Where(item => item.CustomerId == customerId).OrderBy(item => item.Id),
            pagination,
            item => new VehicleResponse(item.Id, item.CustomerId, item.RegistrationNumber),
            cancellationToken);

    public Task<PagedResponse<DealershipResponse>> GetDealershipsAsync(
        PaginationRequest pagination, CancellationToken cancellationToken) =>
        PageAsync(
            dbContext.Dealerships.AsNoTracking().OrderBy(item => item.Id),
            pagination,
            item => new DealershipResponse(item.Id, item.Name, item.TimeZoneId),
            cancellationToken);

    public Task<PagedResponse<ServiceTypeResponse>> GetServiceTypesAsync(
        PaginationRequest pagination, CancellationToken cancellationToken) =>
        PageAsync(
            dbContext.ServiceTypes.AsNoTracking().OrderBy(item => item.Id),
            pagination,
            item => new ServiceTypeResponse(item.Id, item.Name, item.DurationMinutes),
            cancellationToken);

    private static async Task<PagedResponse<TResponse>> PageAsync<TEntity, TResponse>(
        IQueryable<TEntity> query,
        PaginationRequest pagination,
        Expression<Func<TEntity, TResponse>> selector,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pagination.PageSize);
        return new PagedResponse<TResponse>(items, pagination.Page, pagination.PageSize, totalCount, totalPages);
    }
}
