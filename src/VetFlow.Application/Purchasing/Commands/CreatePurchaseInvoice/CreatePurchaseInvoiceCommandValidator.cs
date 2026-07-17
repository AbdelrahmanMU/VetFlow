using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Commands.CreatePurchaseInvoice;

/// <summary>
/// Front-line validation of the create request (REQ-PUR-003, AC-PUR-007, BR-PUR-001):
/// the supplier name and invoice date are required, each producing its own
/// field-keyed error so the UI highlights exactly what is missing; the optional
/// reference and notes only carry length caps. Messages are resource keys; the API
/// middleware is the single translation point. The domain aggregate re-enforces
/// every rule as the backstop (STD-BE-010).
/// </summary>
public sealed class CreatePurchaseInvoiceCommandValidator : AbstractValidator<CreatePurchaseInvoiceCommand>
{
    private const int SupplierNameMaxLength = 300;
    private const int ReferenceMaxLength = 100;
    private const int NotesMaxLength = 2000;

    public CreatePurchaseInvoiceCommandValidator()
    {
        RuleFor(command => command.SupplierName)
            .NotEmpty().WithMessage(ValidationMessageKeys.SupplierNameRequired)
            .MaximumLength(SupplierNameMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);

        RuleFor(command => command.SupplierInvoiceReference)
            .MaximumLength(ReferenceMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);

        RuleFor(command => command.Notes)
            .MaximumLength(NotesMaxLength).WithMessage(ValidationMessageKeys.TextTooLong);

        RuleFor(command => command.InvoiceDate)
            .NotNull().WithMessage(ValidationMessageKeys.InvoiceDateRequired);
    }
}
