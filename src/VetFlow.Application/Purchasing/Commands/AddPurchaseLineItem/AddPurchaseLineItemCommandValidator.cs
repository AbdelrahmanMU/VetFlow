using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Commands.AddPurchaseLineItem;

/// <summary>
/// Front-line validation of an add-line request (REQ-PUR-004, AC-PUR-009, BR-PUR-005):
/// a product and a purchase unit are required, the quantity must be greater than zero,
/// and the unit price must be zero or more — each producing its own field-keyed error
/// so the dialog highlights exactly what is wrong. Messages are resource keys; the API
/// middleware is the single translation point. The domain aggregate re-enforces the
/// numeric rules as the backstop (STD-BE-010).
/// </summary>
public sealed class AddPurchaseLineItemCommandValidator : AbstractValidator<AddPurchaseLineItemCommand>
{
    public AddPurchaseLineItemCommandValidator()
    {
        RuleFor(command => command.ProductId)
            .NotEmpty().WithMessage(ValidationMessageKeys.LineProductRequired);

        RuleFor(command => command.PurchaseUnitId)
            .NotEmpty().WithMessage(ValidationMessageKeys.LinePurchaseUnitRequired);

        RuleFor(command => command.Quantity)
            .GreaterThan(0m).WithMessage(ValidationMessageKeys.LineQuantityPositive);

        RuleFor(command => command.UnitPrice)
            .GreaterThanOrEqualTo(0m).WithMessage(ValidationMessageKeys.LineUnitPriceNonNegative);
    }
}
