using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Queries.InventoryProjection;

/// <summary>Input validation for the inventory projection query (STD-BE-027). Messages are resource keys.</summary>
public sealed class InventoryProjectionQueryValidator : AbstractValidator<InventoryProjectionQuery>
{
    public InventoryProjectionQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage(ValidationMessageKeys.PageMin);

        RuleFor(query => query.Page)
            .LessThanOrEqualTo(InventoryProjectionQuery.MaxPage)
            .WithMessage(ValidationMessageKeys.PageMax);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, InventoryProjectionQuery.MaxPageSize)
            .WithMessage(ValidationMessageKeys.PageSizeRange);

        RuleFor(query => query.Search)
            .MaximumLength(InventoryProjectionQuery.MaxSearchLength)
            .WithMessage(ValidationMessageKeys.SearchMaxLength);
    }
}
