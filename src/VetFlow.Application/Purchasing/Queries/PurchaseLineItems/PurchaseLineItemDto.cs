using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Queries.PurchaseLineItems;

/// <summary>
/// One line of a purchase invoice in the read model (REQ-PUR-004, screen "بنود
/// الفاتورة"): the product and purchase-unit name snapshots (BR-PUR-007), the
/// quantity, the unit price, and the line total (BR-PUR-005). The referenced ids are
/// carried for future linkage; display uses the snapshots.
/// </summary>
public sealed record PurchaseLineItemDto
{
    public required Guid Id { get; init; }

    public required Guid ProductId { get; init; }

    public required string ProductName { get; init; }

    public required Guid PurchaseUnitId { get; init; }

    public required string PurchaseUnitName { get; init; }

    public required decimal Quantity { get; init; }

    public required MoneyDto UnitPrice { get; init; }

    public required MoneyDto LineTotal { get; init; }

    /// <summary>
    /// Whether the line's product currently requires an expiry date at receiving (BR-PUR-013,
    /// DEC-PUR-009). A <b>live</b> read of the current product definition — not a snapshot — so the
    /// receive dialog can mark the expiry field required; the receive handler enforces it as the
    /// backstop (VTF-PUR-007).
    /// </summary>
    public required bool RequiresExpiry { get; init; }
}
