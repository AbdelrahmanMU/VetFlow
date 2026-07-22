namespace VetFlow.Domain.Inventory;

/// <summary>
/// One received lot of a product (BR-INV-001, write-kernel.md), created when a purchase invoice
/// is received (BR-PUR-010): exactly one batch per purchase line. Quantities are in the product's
/// canonical stock unit — receiving converts them before construction (owner ruling 2026-07-22).
/// The batch stores only the minimal fields: the product, the originating purchase line
/// (provenance), the received <see cref="Quantity"/>, <see cref="RemainingQuantity"/> (initialized
/// to the received quantity — no logic consumes it this slice; it exists for forward-compatibility
/// with Sales/FEFO allocation), the unit-cost snapshot (the purchase line's unit price), an optional
/// <see cref="ExpiryDate"/> (DEC-PUR-009), and the receive timestamp. The write kernel owns no
/// queries, projection, adjustments, or movement history (BR-INV-004).
/// </summary>
public sealed class InventoryBatch
{
    private InventoryBatch()
    {
        // EF Core materialization only.
    }

    public InventoryBatch(
        Guid id,
        Guid productId,
        Guid purchaseLineId,
        decimal quantity,
        decimal unitCostSnapshot,
        DateOnly? expiryDate,
        DateTimeOffset receivedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(productId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(purchaseLineId, Guid.Empty);
        // Inputs are already validated by receiving (BR-PUR-005/012); the batch re-enforces them
        // as the backstop (STD-BE-010).
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitCostSnapshot);

        Id = id;
        ProductId = productId;
        PurchaseLineId = purchaseLineId;
        Quantity = quantity;
        RemainingQuantity = quantity;
        UnitCostSnapshot = unitCostSnapshot;
        ExpiryDate = expiryDate;
        ReceivedAt = receivedAt;
    }

    public Guid Id { get; }

    /// <summary>The received product (kept for future linkage; no cross-module FK).</summary>
    public Guid ProductId { get; }

    /// <summary>The purchase line this batch was created from — provenance (BR-PUR-010).</summary>
    public Guid PurchaseLineId { get; }

    /// <summary>Received quantity in the product's canonical stock unit (BR-INV-001).</summary>
    public decimal Quantity { get; }

    /// <summary>Initialized to <see cref="Quantity"/>; no logic consumes it this slice (forward-compat: Sales/FEFO).</summary>
    public decimal RemainingQuantity { get; private set; }

    /// <summary>Snapshot of the purchase line's unit price at receiving (BR-INV-001).</summary>
    public decimal UnitCostSnapshot { get; }

    /// <summary>Optional expiry date; null when the product does not require expiry (DEC-PUR-009).</summary>
    public DateOnly? ExpiryDate { get; }

    /// <summary>System timestamp at receiving (BR-INV-001).</summary>
    public DateTimeOffset ReceivedAt { get; }
}
