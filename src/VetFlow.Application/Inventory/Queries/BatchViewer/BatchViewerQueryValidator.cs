using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Queries.BatchViewer;

/// <summary>Input validation for the batch viewer query (STD-BE-027). Messages are resource keys.</summary>
public sealed class BatchViewerQueryValidator : AbstractValidator<BatchViewerQuery>
{
    public BatchViewerQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage(ValidationMessageKeys.PageMin);

        RuleFor(query => query.Page)
            .LessThanOrEqualTo(BatchViewerQuery.MaxPage)
            .WithMessage(ValidationMessageKeys.PageMax);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, BatchViewerQuery.MaxPageSize)
            .WithMessage(ValidationMessageKeys.PageSizeRange);
    }
}
