using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.CommitSalesInvoice;

/// <summary>
/// Validates the shape of a commit command (REQ-SAL-003). The business preconditions — draft state
/// and at least one line (BR-SAL-009), saleable-stock sufficiency (BR-SAL-012 / BR-INV-052), exact
/// unit conversion (BR-INV-058), and concurrency (BR-INV-056) — are enforced in the handler, the
/// aggregate, and the Inventory consumption contract, where the invoice, its products, and the
/// batches are loaded.
/// </summary>
public sealed class CommitSalesInvoiceCommandValidator : AbstractValidator<CommitSalesInvoiceCommand>
{
    public CommitSalesInvoiceCommandValidator()
    {
        RuleFor(command => command.InvoiceId)
            .NotEmpty().WithMessage(ValidationMessageKeys.InvalidId);
    }
}
