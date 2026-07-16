using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Queries.ManufacturerList;

/// <summary>Input validation for the manufacturer list query (STD-BE-027). Messages are resource keys.</summary>
public sealed class ManufacturerListQueryValidator : AbstractValidator<ManufacturerListQuery>
{
    public ManufacturerListQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage(ValidationMessageKeys.PageMin);

        RuleFor(query => query.Page)
            .LessThanOrEqualTo(ManufacturerListQuery.MaxPage)
            .WithMessage(ValidationMessageKeys.PageMax);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, ManufacturerListQuery.MaxPageSize)
            .WithMessage(ValidationMessageKeys.PageSizeRange);

        RuleFor(query => query.Search)
            .MaximumLength(ManufacturerListQuery.MaxSearchLength)
            .WithMessage(ValidationMessageKeys.SearchMaxLength);
    }
}
