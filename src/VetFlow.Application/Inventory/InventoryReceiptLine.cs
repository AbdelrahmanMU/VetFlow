namespace VetFlow.Application.Inventory;

/// <summary>
/// One received line handed to the Inventory write kernel (write-kernel.md, BR-INV-001). The
/// <see cref="StockQuantity"/> is already in the product's canonical stock unit — receiving converts
/// it before calling the kernel (owner ruling 2026-07-22). Carries no Purchasing or Catalog type
/// (module isolation): only the ids and values the kernel persists.
/// </summary>
public sealed record InventoryReceiptLine
{
    public required Guid ProductId { get; init; }

    /// <summary>The purchase line this batch originates from (provenance, BR-PUR-010).</summary>
    public required Guid PurchaseLineId { get; init; }

    /// <summary>Received quantity in the product's canonical stock unit (already converted).</summary>
    public required decimal StockQuantity { get; init; }

    /// <summary>Snapshot of the purchase line's unit price.</summary>
    public required decimal UnitCost { get; init; }

    /// <summary>Optional expiry date; null when the product does not require expiry (DEC-PUR-009).</summary>
    public DateOnly? ExpiryDate { get; init; }

    public required DateTimeOffset ReceivedAt { get; init; }
}
