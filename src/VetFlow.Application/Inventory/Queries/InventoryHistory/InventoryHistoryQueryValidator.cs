using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Queries.InventoryHistory;

/// <summary>Input validation for the inventory history query (STD-BE-027). Messages are resource keys.</summary>
public sealed class InventoryHistoryQueryValidator : AbstractValidator<InventoryHistoryQuery>
{
    public InventoryHistoryQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage(ValidationMessageKeys.PageMin);

        RuleFor(query => query.Page)
            .LessThanOrEqualTo(InventoryHistoryQuery.MaxPage)
            .WithMessage(ValidationMessageKeys.PageMax);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, InventoryHistoryQuery.MaxPageSize)
            .WithMessage(ValidationMessageKeys.PageSizeRange);
    }
}
