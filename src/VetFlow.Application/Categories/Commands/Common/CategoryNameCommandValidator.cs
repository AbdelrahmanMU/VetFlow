using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Categories.Commands.Common;

/// <summary>
/// Front-line validation shared by every category write command — create and
/// rename (REQ-CTG-002/003). The name is mandatory and length-capped (BR-CTG-002 —
/// the 100-character cap is an engineering constraint, TD-006 style). Uniqueness
/// (BR-CTG-003) is NOT here: it needs the database, and the validators are
/// registered as singletons, so a scoped DbContext cannot be injected — the
/// handler enforces uniqueness instead. Messages are resource keys; the API
/// middleware is the single translation point.
/// </summary>
public abstract class CategoryNameCommandValidator<TCommand> : AbstractValidator<TCommand>
    where TCommand : ICategoryNameCommand
{
    private const int NameMaxLength = 100;

    protected CategoryNameCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty().WithMessage(ValidationMessageKeys.CategoryNameRequired)
            .MaximumLength(NameMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);
    }
}
