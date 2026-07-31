using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Queries.SalesList;

/// <summary>Input validation for the sales list query (STD-BE-027). Messages are resource keys.</summary>
public sealed class SalesListQueryValidator : AbstractValidator<SalesListQuery>
{
    public SalesListQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage(ValidationMessageKeys.PageMin);

        RuleFor(query => query.Page)
            .LessThanOrEqualTo(SalesListQuery.MaxPage)
            .WithMessage(ValidationMessageKeys.PageMax);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, SalesListQuery.MaxPageSize)
            .WithMessage(ValidationMessageKeys.PageSizeRange);

        RuleFor(query => query.Search)
            .MaximumLength(SalesListQuery.MaxSearchLength)
            .WithMessage(ValidationMessageKeys.SearchMaxLength);
    }
}
