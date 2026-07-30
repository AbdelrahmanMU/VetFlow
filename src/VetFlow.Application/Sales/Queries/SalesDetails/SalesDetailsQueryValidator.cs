using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Queries.SalesDetails;

/// <summary>Input validation for the sales-details query. Messages are resource keys.</summary>
public sealed class SalesDetailsQueryValidator : AbstractValidator<SalesDetailsQuery>
{
    public SalesDetailsQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty().WithMessage(ValidationMessageKeys.InvalidId);
    }
}
