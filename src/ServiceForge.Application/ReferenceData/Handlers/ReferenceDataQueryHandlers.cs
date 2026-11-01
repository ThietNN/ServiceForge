using FluentValidation;
using MediatR;
using ServiceForge.Application.Abstractions;
using ServiceForge.Application.Common.Pagination;
using ServiceForge.Application.ReferenceData.Contracts;
using ServiceForge.Application.ReferenceData.Queries;

namespace ServiceForge.Application.ReferenceData.Handlers;

public sealed class GetCustomerVehiclesQueryHandler(IReferenceDataReader reader, IValidator<PaginationRequest> validator)
    : IRequestHandler<GetCustomerVehiclesQuery, PagedResponse<VehicleResponse>>
{
    public async Task<PagedResponse<VehicleResponse>> Handle(GetCustomerVehiclesQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request.Pagination, cancellationToken);
        return await reader.GetCustomerVehiclesAsync(request.CustomerId, request.Pagination, cancellationToken);
    }
}

public sealed class GetDealershipsQueryHandler(IReferenceDataReader reader, IValidator<PaginationRequest> validator)
    : IRequestHandler<GetDealershipsQuery, PagedResponse<DealershipResponse>>
{
    public async Task<PagedResponse<DealershipResponse>> Handle(GetDealershipsQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request.Pagination, cancellationToken);
        return await reader.GetDealershipsAsync(request.Pagination, cancellationToken);
    }
}

public sealed class GetServiceTypesQueryHandler(IReferenceDataReader reader, IValidator<PaginationRequest> validator)
    : IRequestHandler<GetServiceTypesQuery, PagedResponse<ServiceTypeResponse>>
{
    public async Task<PagedResponse<ServiceTypeResponse>> Handle(GetServiceTypesQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request.Pagination, cancellationToken);
        return await reader.GetServiceTypesAsync(request.Pagination, cancellationToken);
    }
}
