using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.AddSalesLineItem;

/// <summary>
/// Front-line validation of an add-line request (REQ-SAL-001, AC-SAL-003, BR-SAL-004): a product
/// and a sale unit are required and the quantity must be greater than zero — each producing its own
/// field-keyed error so the dialog highlights exactly what is wrong. The rules that need the
/// catalog — the unit must be a <b>sale</b> unit, a sale price must be defined for it, and the
/// splittability constraint (DEC-SAL-007) — are enforced in the handler and the aggregate, where
/// the product is loaded. Messages are resource keys; the API middleware is the single translation
/// point.
/// </summary>
public sealed class AddSalesLineItemCommandValidator : AbstractValidator<AddSalesLineItemCommand>
{
    public AddSalesLineItemCommandValidator()
    {
        RuleFor(command => command.InvoiceId)
            .NotEmpty().WithMessage(ValidationMessageKeys.InvalidId);

        RuleFor(command => command.ProductId)
            .NotEmpty().WithMessage(ValidationMessageKeys.LineProductRequired);

        RuleFor(command => command.SaleUnitId)
            .NotEmpty().WithMessage(ValidationMessageKeys.LineSaleUnitRequired);

        RuleFor(command => command.Quantity)
            .GreaterThan(0m).WithMessage(ValidationMessageKeys.LineQuantityPositive);
    }
}
