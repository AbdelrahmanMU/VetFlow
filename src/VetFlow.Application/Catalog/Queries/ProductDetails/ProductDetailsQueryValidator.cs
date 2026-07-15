using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Queries.ProductDetails;

/// <summary>Input validation for the product-details query. Messages are resource keys.</summary>
public sealed class ProductDetailsQueryValidator : AbstractValidator<ProductDetailsQuery>
{
    public ProductDetailsQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty().WithMessage(ValidationMessageKeys.InvalidId);
    }
}
