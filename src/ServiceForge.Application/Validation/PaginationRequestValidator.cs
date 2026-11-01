using FluentValidation;
using ServiceForge.Application.Common.Pagination;

namespace ServiceForge.Application.Validation;

public sealed class PaginationRequestValidator : AbstractValidator<PaginationRequest>
{
    public PaginationRequestValidator()
    {
        RuleFor(request => request.Page).GreaterThanOrEqualTo(1);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
    }
}
