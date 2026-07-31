using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Commands.AddPurchaseReturnLine;

/// <summary>
/// Front-line validation of an added return line (BR-PUR-016, AC-PUR-021): the original line is
/// required and the quantity must be positive, each with its own field key. The <b>ceiling</b> —
/// the remaining returnable quantity — is deliberately not here: it depends on other committed
/// return documents, so it is a business rule raising VTF-PUR-016, not a per-field validation.
/// </summary>
public sealed class AddPurchaseReturnLineCommandValidator : AbstractValidator<AddPurchaseReturnLineCommand>
{
    public AddPurchaseReturnLineCommandValidator()
    {
        RuleFor(command => command.PurchaseLineItemId)
            .NotNull().WithMessage(ValidationMessageKeys.ReturnOriginalLineRequired)
            .NotEqual(Guid.Empty).WithMessage(ValidationMessageKeys.ReturnOriginalLineRequired);

        RuleFor(command => command.Quantity)
            .NotNull().WithMessage(ValidationMessageKeys.ReturnQuantityPositive)
            .GreaterThan(0m).WithMessage(ValidationMessageKeys.ReturnQuantityPositive);
    }
}
