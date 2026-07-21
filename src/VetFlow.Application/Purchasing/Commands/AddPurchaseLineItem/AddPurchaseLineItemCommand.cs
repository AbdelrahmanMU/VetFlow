using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Commands.AddPurchaseLineItem;

/// <summary>
/// Add a line item to a draft purchase invoice (REQ-PUR-004, BR-PUR-005). The
/// command carries ids only — the product and its purchase unit are referenced by
/// id; their names are resolved to a catalog snapshot in the handler (BR-PUR-007).
/// Quantity and unit price are validated field-by-field (AC-PUR-009) and the
/// aggregate recomputes the invoice total (BR-PUR-006). A <c>null</c> result means
/// the invoice does not exist (404, AC-PUR-011); a non-null result is the new line id.
/// </summary>
public sealed record AddPurchaseLineItemCommand : ICommand<Guid?>
{
    public required Guid InvoiceId { get; init; }

    public required Guid ProductId { get; init; }

    public required Guid PurchaseUnitId { get; init; }

    public required decimal Quantity { get; init; }

    public required decimal UnitPrice { get; init; }
}
