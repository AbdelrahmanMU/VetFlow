using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Commands.Common;

/// <summary>
/// The shared front-line validation for every product write command — create and
/// update (REQ-CAT-043, AC-CAT-043). Each missing mandatory-minimum field
/// (BR-CAT-009) produces its own field-keyed error so the UI can highlight exactly
/// what is missing (ui.md §5). Defined once here; concrete validators derive from
/// it so the rules are never duplicated (DEC-CAT-031). The domain aggregate
/// re-enforces every rule as the backstop (STD-BE-010). Messages are resource
/// keys; the API middleware is the single translation point.
/// </summary>
public abstract class ProductWriteCommandValidator<TCommand> : AbstractValidator<TCommand>
    where TCommand : IProductWriteCommand
{
    private const int NameMaxLength = 300;
    private const int ShortTextMaxLength = 100;
    private const int NotesMaxLength = 2000;

    protected ProductWriteCommandValidator()
    {
        RuleFor(command => command.ArabicName)
            .NotEmpty().WithMessage(ValidationMessageKeys.ArabicNameRequired)
            .MaximumLength(NameMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);

        RuleFor(command => command.EnglishName)
            .MaximumLength(NameMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);

        RuleFor(command => command.Size)
            .MaximumLength(ShortTextMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);

        RuleFor(command => command.Concentration)
            .MaximumLength(ShortTextMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);

        RuleFor(command => command.InternalNotes)
            .MaximumLength(NotesMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);

        RuleFor(command => command.CategoryId)
            .NotEmpty().WithMessage(ValidationMessageKeys.CategoryRequired);

        RuleFor(command => command.ManufacturerId)
            .NotEmpty().WithMessage(ValidationMessageKeys.ManufacturerRequired);

        RuleFor(command => command.NatureId)
            .NotEmpty().WithMessage(ValidationMessageKeys.NatureRequired);

        RuleFor(command => command.Units)
            .NotEmpty().WithMessage(ValidationMessageKeys.UnitProfileRequired);

        RuleFor(command => command.Units)
            .Must(units => units is not null && units.Any(unit => unit.IsPurchaseUnit))
            .WithMessage(ValidationMessageKeys.PurchaseUnitRequired)
            .When(command => command.Units is { Count: > 0 });

        RuleFor(command => command.Units)
            .Must(units => units is not null && units.Any(unit => unit.IsSaleUnit))
            .WithMessage(ValidationMessageKeys.SaleUnitRequired)
            .When(command => command.Units is { Count: > 0 });

        RuleFor(command => command.StorageUnitId)
            .NotEmpty().WithMessage(ValidationMessageKeys.StorageUnitRequired);

        RuleFor(command => command.DefaultSaleUnitId)
            .NotEmpty().WithMessage(ValidationMessageKeys.DefaultSaleUnitRequired);

        RuleFor(command => command.DefaultPurchaseUnitId)
            .NotEmpty().WithMessage(ValidationMessageKeys.DefaultPurchaseUnitRequired);

        RuleFor(command => command.OpenExpirationPeriodDays)
            .Must(days => days is > 0)
            .WithMessage(ValidationMessageKeys.OpenExpirationPeriodRequired)
            .When(command => command.HasOpenExpiration);

        RuleForEach(command => command.Units).ChildRules(unit =>
        {
            unit.RuleFor(row => row.QuantityInNextUnit)
                .Must(quantity => quantity is null or > 0)
                .WithMessage(ValidationMessageKeys.ConversionFactorPositive);

            unit.RuleFor(row => row.Barcode)
                .MaximumLength(ShortTextMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);
        });
    }
}
