using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.CreateSalesInvoice;

/// <summary>
/// Front-line validation of the create request (REQ-SAL-001, AC-SAL-002, BR-SAL-001): the sale date
/// is the <b>only</b> required field — the customer name is optional (DEC-SAL-002) and notes are
/// optional, both carrying length caps only. Each failure is field-keyed so the UI highlights
/// exactly what is missing. Messages are resource keys; the API middleware is the single
/// translation point. The domain aggregate re-enforces every rule as the backstop (STD-BE-010).
/// </summary>
public sealed class CreateSalesInvoiceCommandValidator : AbstractValidator<CreateSalesInvoiceCommand>
{
    private const int CustomerNameMaxLength = 300;
    private const int NotesMaxLength = 2000;

    public CreateSalesInvoiceCommandValidator()
    {
        RuleFor(command => command.CustomerName)
            .MaximumLength(CustomerNameMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);

        RuleFor(command => command.Notes)
            .MaximumLength(NotesMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);

        RuleFor(command => command.SaleDate)
            .NotNull().WithMessage(ValidationMessageKeys.SaleDateRequired);
    }
}
