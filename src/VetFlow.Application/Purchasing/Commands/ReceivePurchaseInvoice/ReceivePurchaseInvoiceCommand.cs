using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Commands.ReceivePurchaseInvoice;

/// <summary>
/// Receive a draft purchase invoice (REQ-PUR-005): transition Draft → Received and persist the
/// inventory effect atomically (BR-PUR-009..012, BR-PUR-010). It carries an optional expiry date
/// per line; the handler enforces it as <b>required</b> for a line whose product requires expiry
/// (BR-PUR-013, DEC-PUR-009), and ignores it for a product that does not. Returns the invoice id,
/// or <c>null</c> when the invoice does not exist (404).
/// </summary>
public sealed record ReceivePurchaseInvoiceCommand : ICommand<Guid?>
{
    public required Guid InvoiceId { get; init; }

    public required IReadOnlyList<ReceiveLineExpiry> LineExpiries { get; init; }
}

/// <summary>An optional expiry date captured for one line at receiving (BR-PUR-013).</summary>
public sealed record ReceiveLineExpiry
{
    public required Guid LineId { get; init; }

    public DateOnly? ExpiryDate { get; init; }
}
