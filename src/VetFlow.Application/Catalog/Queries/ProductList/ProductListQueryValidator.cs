using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Queries.ProductList;

/// <summary>Input validation for the product list query (STD-BE-027). Messages are resource keys.</summary>
public sealed class ProductListQueryValidator : AbstractValidator<ProductListQuery>
{
    public ProductListQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage(ValidationMessageKeys.PageMin);

        RuleFor(query => query.Page)
            .LessThanOrEqualTo(ProductListQuery.MaxPage)
            .WithMessage(ValidationMessageKeys.PageMax);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, ProductListQuery.MaxPageSize)
            .WithMessage(ValidationMessageKeys.PageSizeRange);

        RuleFor(query => query.Search)
            .MaximumLength(ProductListQuery.MaxSearchLength)
            .WithMessage(ValidationMessageKeys.SearchMaxLength);
    }
}
