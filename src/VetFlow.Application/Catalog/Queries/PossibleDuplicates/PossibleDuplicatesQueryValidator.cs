using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Queries.PossibleDuplicates;

/// <summary>Input validation for the possible-duplicate advisory read. Messages are resource keys.</summary>
public sealed class PossibleDuplicatesQueryValidator : AbstractValidator<PossibleDuplicatesQuery>
{
    public PossibleDuplicatesQueryValidator()
    {
        RuleFor(query => query.ArabicName)
            .NotEmpty().WithMessage(ValidationMessageKeys.DuplicateArabicNameRequired);

        RuleFor(query => query.ManufacturerId)
            .NotEmpty().WithMessage(ValidationMessageKeys.DuplicateManufacturerRequired);
    }
}
