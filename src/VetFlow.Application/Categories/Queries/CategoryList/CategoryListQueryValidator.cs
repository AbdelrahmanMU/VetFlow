using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Categories.Queries.CategoryList;

/// <summary>Input validation for the category list query (STD-BE-027). Messages are resource keys.</summary>
public sealed class CategoryListQueryValidator : AbstractValidator<CategoryListQuery>
{
    public CategoryListQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage(ValidationMessageKeys.PageMin);

        RuleFor(query => query.Page)
            .LessThanOrEqualTo(CategoryListQuery.MaxPage)
            .WithMessage(ValidationMessageKeys.PageMax);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, CategoryListQuery.MaxPageSize)
            .WithMessage(ValidationMessageKeys.PageSizeRange);

        RuleFor(query => query.Search)
            .MaximumLength(CategoryListQuery.MaxSearchLength)
            .WithMessage(ValidationMessageKeys.SearchMaxLength);
    }
}
