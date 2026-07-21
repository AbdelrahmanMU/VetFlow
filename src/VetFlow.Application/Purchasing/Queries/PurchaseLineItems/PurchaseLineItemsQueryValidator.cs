using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Queries.PurchaseLineItems;

/// <summary>Input validation for the purchase-line-items query. Messages are resource keys.</summary>
public sealed class PurchaseLineItemsQueryValidator : AbstractValidator<PurchaseLineItemsQuery>
{
    public PurchaseLineItemsQueryValidator()
    {
        RuleFor(query => query.InvoiceId)
            .NotEmpty().WithMessage(ValidationMessageKeys.InvalidId);
    }
}
