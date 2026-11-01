using ServiceForge.Application.Common.Pagination;
using ServiceForge.Application.ReferenceData.Contracts;

namespace ServiceForge.Application.Abstractions;

public interface IReferenceDataReader
{
    Task<PagedResponse<VehicleResponse>> GetCustomerVehiclesAsync(
        long customerId,
        PaginationRequest pagination,
        CancellationToken cancellationToken);

    Task<PagedResponse<DealershipResponse>> GetDealershipsAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken);

    Task<PagedResponse<ServiceTypeResponse>> GetServiceTypesAsync(
        PaginationRequest pagination,
        CancellationToken cancellationToken);
}
