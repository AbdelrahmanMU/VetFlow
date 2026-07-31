using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.AddSalesReturnLine;

/// <summary>
/// Front-line validation of an added return line (BR-SAL-016, AC-SAL-016): the original sale line is
/// required and the quantity must be positive, each with its own field key. Two things are
/// deliberately not here, because neither is a per-field check: the <b>ceiling</b> (it depends on
/// other committed return documents — VTF-SAL-016) and the <b>splittability</b> constraint (it
/// depends on the product's catalog configuration — VTF-SAL-017).
/// </summary>
public sealed class AddSalesReturnLineCommandValidator : AbstractValidator<AddSalesReturnLineCommand>
{
    public AddSalesReturnLineCommandValidator()
    {
        RuleFor(command => command.SalesLineItemId)
            .NotNull().WithMessage(ValidationMessageKeys.ReturnOriginalLineRequired)
            .NotEqual(Guid.Empty).WithMessage(ValidationMessageKeys.ReturnOriginalLineRequired);

        RuleFor(command => command.Quantity)
            .NotNull().WithMessage(ValidationMessageKeys.ReturnQuantityPositive)
            .GreaterThan(0m).WithMessage(ValidationMessageKeys.ReturnQuantityPositive);
    }
}
