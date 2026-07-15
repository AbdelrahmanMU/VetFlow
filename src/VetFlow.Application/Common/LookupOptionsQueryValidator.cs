using FluentValidation;

namespace VetFlow.Application.Common;

/// <summary>Shared validation of the lookup option queries. Messages are resource keys.</summary>
public abstract class LookupOptionsQueryValidator<TQuery> : AbstractValidator<TQuery>
    where TQuery : LookupOptionsQuery
{
    protected LookupOptionsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage(ValidationMessageKeys.PageMin);

        RuleFor(query => query.Page)
            .LessThanOrEqualTo(LookupOptionsQuery.MaxPage)
            .WithMessage(ValidationMessageKeys.PageMax);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, LookupOptionsQuery.MaxPageSize)
            .WithMessage(ValidationMessageKeys.PageSizeRange);

        RuleFor(query => query.Search)
            .MaximumLength(LookupOptionsQuery.MaxSearchLength)
            .WithMessage(ValidationMessageKeys.SearchMaxLength);
    }
}
