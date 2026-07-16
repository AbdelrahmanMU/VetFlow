using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Queries.PurchaseList;

/// <summary>Input validation for the purchase list query (STD-BE-027). Messages are resource keys.</summary>
public sealed class PurchaseListQueryValidator : AbstractValidator<PurchaseListQuery>
{
    public PurchaseListQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage(ValidationMessageKeys.PageMin);

        RuleFor(query => query.Page)
            .LessThanOrEqualTo(PurchaseListQuery.MaxPage)
            .WithMessage(ValidationMessageKeys.PageMax);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PurchaseListQuery.MaxPageSize)
            .WithMessage(ValidationMessageKeys.PageSizeRange);

        RuleFor(query => query.Search)
            .MaximumLength(PurchaseListQuery.MaxSearchLength)
            .WithMessage(ValidationMessageKeys.SearchMaxLength);
    }
}
