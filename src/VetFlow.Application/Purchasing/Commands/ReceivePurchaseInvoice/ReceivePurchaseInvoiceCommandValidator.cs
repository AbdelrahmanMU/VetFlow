using FluentValidation;

namespace VetFlow.Application.Purchasing.Commands.ReceivePurchaseInvoice;

/// <summary>
/// Validates the shape of a receive command (REQ-PUR-005). The business preconditions — draft
/// state, at least one line (BR-PUR-012), and the product-driven expiry requirement (BR-PUR-013) —
/// are enforced in the handler and the aggregate, where the invoice and its products are loaded.
/// </summary>
public sealed class ReceivePurchaseInvoiceCommandValidator : AbstractValidator<ReceivePurchaseInvoiceCommand>
{
    public ReceivePurchaseInvoiceCommandValidator()
    {
        RuleFor(command => command.InvoiceId).NotEmpty();
        RuleFor(command => command.LineExpiries).NotNull();
    }
}
