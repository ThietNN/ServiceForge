using MediatR;
using ServiceForge.Application.Common.Pagination;
using ServiceForge.Application.ReferenceData.Contracts;

namespace ServiceForge.Application.ReferenceData.Queries;

public sealed record GetCustomerVehiclesQuery(long CustomerId, PaginationRequest Pagination)
    : IRequest<PagedResponse<VehicleResponse>>;
