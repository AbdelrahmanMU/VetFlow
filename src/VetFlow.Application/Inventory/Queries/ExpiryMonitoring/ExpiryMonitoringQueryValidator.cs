using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Queries.ExpiryMonitoring;

/// <summary>Input validation for the expiry monitoring query (STD-BE-027). Messages are resource keys.</summary>
public sealed class ExpiryMonitoringQueryValidator : AbstractValidator<ExpiryMonitoringQuery>
{
    public ExpiryMonitoringQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage(ValidationMessageKeys.PageMin);

        RuleFor(query => query.Page)
            .LessThanOrEqualTo(ExpiryMonitoringQuery.MaxPage)
            .WithMessage(ValidationMessageKeys.PageMax);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, ExpiryMonitoringQuery.MaxPageSize)
            .WithMessage(ValidationMessageKeys.PageSizeRange);

        RuleFor(query => query.Search)
            .MaximumLength(ExpiryMonitoringQuery.MaxSearchLength)
            .WithMessage(ValidationMessageKeys.SearchMaxLength);
    }
}
