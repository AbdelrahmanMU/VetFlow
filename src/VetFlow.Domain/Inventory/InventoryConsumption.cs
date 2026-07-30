namespace VetFlow.Domain.Inventory;

/// <summary>
/// The record that keeps one inventory consumption <b>traceable</b> (REQ-INV-008, BR-INV-057):
/// one row per (sale line, batch) pair, carrying the quantity taken from that batch in the
/// product's stock unit. It makes the required navigation possible — <b>sale line → the consumed
/// batch(es), with the quantity from each</b> — at the level the owner ruled: the <b>sale line</b>,
/// not the invoice, so future Returns can tell which line consumed which batch.
///
/// It is written at commit time, inside the same unit of work as the batch decrements
/// (BR-INV-048), because the relationship cannot be derived afterwards from
/// <see cref="InventoryBatch.RemainingQuantity"/> alone — what is not written then is lost forever.
///
/// This is <b>not</b> a movement ledger and not the deferred Inventory History feature
/// (DEC-INV-015 stays deferred): it has no movement type, no source module, and no screen. It is
/// the minimal proof the traceability requirement demands, and the model was left to
/// implementation by explicit owner ruling.
///
/// <see cref="SaleLineId"/> is a plain <see cref="Guid"/> with no cross-module foreign key — the
/// exact precedent set by <see cref="InventoryBatch.PurchaseLineId"/> (BR-PUR-010). The data is
/// owned by Inventory, so Sales still knows nothing about batches (BR-SAL-013).
/// </summary>
public sealed class InventoryConsumption
{
    private InventoryConsumption()
    {
        // EF Core materialization only.
    }

    public InventoryConsumption(
        Guid id,
        Guid batchId,
        Guid productId,
        Guid saleLineId,
        decimal quantity,
        DateTimeOffset consumedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(batchId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(productId, Guid.Empty);
        // Traceability information is a precondition of accepting a consumption request
        // (BR-INV-046): a quantity that cannot be attributed to its originating sale line is
        // never consumed.
        ArgumentOutOfRangeException.ThrowIfEqual(saleLineId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        Id = id;
        BatchId = batchId;
        ProductId = productId;
        SaleLineId = saleLineId;
        Quantity = quantity;
        ConsumedAt = consumedAt;
    }

    public Guid Id { get; }

    /// <summary>The batch the quantity was taken from (BR-INV-057).</summary>
    public Guid BatchId { get; }

    /// <summary>The product consumed — carried so traceability survives without re-reading the batch.</summary>
    public Guid ProductId { get; }

    /// <summary>The originating sale line (REQ-INV-008); a plain id, no cross-module FK.</summary>
    public Guid SaleLineId { get; }

    /// <summary>Quantity taken from this batch, in the product's stock unit — exact, never rounded (BR-INV-058).</summary>
    public decimal Quantity { get; }

    /// <summary>System timestamp at commit (BR-INV-057 — the relationship is fixed at that moment).</summary>
    public DateTimeOffset ConsumedAt { get; }
}
