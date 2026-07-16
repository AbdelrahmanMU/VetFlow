using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Commands.Common;

/// <summary>
/// Front-line validation shared by every manufacturer write command — create and
/// rename (REQ-CAT-013). The name is mandatory and length-capped (the 100-character
/// cap is an engineering constraint, TD-006 style). Uniqueness after Arabic
/// normalization is NOT here: it needs the database, and the validators are
/// registered as singletons, so a scoped DbContext cannot be injected — the handler
/// enforces uniqueness instead. Messages are resource keys; the API middleware is
/// the single translation point.
/// </summary>
public abstract class ManufacturerNameCommandValidator<TCommand> : AbstractValidator<TCommand>
    where TCommand : IManufacturerNameCommand
{
    private const int NameMaxLength = 100;

    protected ManufacturerNameCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage(ValidationMessageKeys.ManufacturerNameRequired)
            .MaximumLength(NameMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);
    }
}
