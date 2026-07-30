using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Queries.SalesLineItems;

/// <summary>Input validation for the sales-line-items query. Messages are resource keys.</summary>
public sealed class SalesLineItemsQueryValidator : AbstractValidator<SalesLineItemsQuery>
{
    public SalesLineItemsQueryValidator()
    {
        RuleFor(query => query.InvoiceId)
            .NotEmpty().WithMessage(ValidationMessageKeys.InvalidId);
    }
}
