using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Queries.PurchaseDetails;

/// <summary>Input validation for the purchase-details query. Messages are resource keys.</summary>
public sealed class PurchaseDetailsQueryValidator : AbstractValidator<PurchaseDetailsQuery>
{
    public PurchaseDetailsQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty().WithMessage(ValidationMessageKeys.InvalidId);
    }
}
