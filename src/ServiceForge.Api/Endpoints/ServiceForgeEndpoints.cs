using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using ServiceForge.Application.Appointments.Contracts;
using ServiceForge.Application.Common.Pagination;
using ServiceForge.Application.ReferenceData.Queries;

namespace ServiceForge.Api.Endpoints;

public static class ServiceForgeEndpoints
{
    public static IEndpointRouteBuilder MapServiceForgeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var appointments = endpoints.MapGroup("/api/appointments");
        appointments.MapPost("", CreateAppointmentAsync).WithName("CreateAppointment");
        appointments.MapGet("/{id:long}", GetAppointmentAsync).WithName("GetAppointment");

        endpoints.MapGet("/api/customers/{id:long}/vehicles", GetCustomerVehiclesAsync);
        endpoints.MapGet("/api/dealerships", GetDealershipsAsync);
        endpoints.MapGet("/api/service-types", GetServiceTypesAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAppointmentAsync(
        CreateAppointmentRequest? request,
        HttpContext context,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ApiProblem.Write(context, 400, "INVALID_REQUEST", "The request body is required.");
        }

        var key = context.Request.Headers["Idempotency-Key"].FirstOrDefault() ?? string.Empty;
        var result = await sender.Send(new CreateAppointmentCommand(request, key), cancellationToken);
        return result.IsSuccess
            ? Results.Created(result.Location, result.Response)
            : ApiProblem.Write(context, result);
    }

    private static async Task<IResult> GetAppointmentAsync(
        long id,
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return ApiProblem.Write(context, 400, "INVALID_REQUEST", "Appointment ID must be positive.");
        }

        var response = await sender.Send(new GetAppointmentQuery(id), cancellationToken);
        return response is null
            ? ApiProblem.Write(context, 404, "RESOURCE_NOT_FOUND", "Appointment was not found.")
            : Results.Ok(response);
    }

    private static Task<IResult> GetCustomerVehiclesAsync(
        long id,
        int? page,
        int? pageSize,
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken) =>
        SendPagedAsync(new GetCustomerVehiclesQuery(id, Pagination(page, pageSize)), sender, context, cancellationToken);

    private static Task<IResult> GetDealershipsAsync(
        int? page,
        int? pageSize,
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken) =>
        SendPagedAsync(new GetDealershipsQuery(Pagination(page, pageSize)), sender, context, cancellationToken);

    private static Task<IResult> GetServiceTypesAsync(
        int? page,
        int? pageSize,
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken) =>
        SendPagedAsync(new GetServiceTypesQuery(Pagination(page, pageSize)), sender, context, cancellationToken);

    private static async Task<IResult> SendPagedAsync<T>(
        IRequest<T> request,
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await sender.Send(request, cancellationToken));
        }
        catch (FluentValidation.ValidationException exception)
        {
            var errors = exception.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
            return ApiProblem.Write(context, 400, "INVALID_REQUEST", "The pagination parameters are invalid.", errors);
        }
    }

    private static PaginationRequest Pagination(int? page, int? pageSize) =>
        new(page ?? 1, pageSize ?? 20);
}
