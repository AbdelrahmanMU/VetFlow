using System.Globalization;
using VetFlow.Domain.Common;

namespace VetFlow.Domain.Inventory;

/// <summary>
/// One received lot of a product (BR-INV-001, write-kernel.md), created when a purchase invoice
/// is received (BR-PUR-010): exactly one batch per purchase line. Quantities are in the product's
/// canonical stock unit — receiving converts them before construction (owner ruling 2026-07-22).
/// The batch stores only the minimal fields: the product, the originating purchase line
/// (provenance), the received <see cref="Quantity"/>, <see cref="RemainingQuantity"/> (initialized
/// to the received quantity — no logic consumes it this slice; it exists for forward-compatibility
/// with Sales/FEFO allocation), the unit-cost snapshot (the purchase line's unit price), an optional
/// <see cref="ExpiryDate"/> (DEC-PUR-009), and the receive timestamp.
///
/// <para>The write kernel itself still owns no queries, projection or reporting (BR-INV-004, as
/// amended for Epic 2). Adjustments, write-off and movement history are <b>separate Inventory
/// paths</b> — <c>BatchOperationWriter</c> and the history projection — operating on the same
/// quantities; not one line was added to receiving.</para>
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

    /// <summary>
    /// Consume part (or all) of this batch's remaining quantity (BR-INV-047) — the write half of
    /// FEFO allocation, and the only path that ever decreases <see cref="RemainingQuantity"/>.
    /// <see cref="Quantity"/>, the historical received amount, never changes; the batch is never
    /// deleted, and a batch drained to zero simply becomes "depleted" by the existing derivation
    /// (BR-INV-021). The remaining quantity can never go below zero: the allocator checks
    /// sufficiency against the saleable batches before writing anything (BR-INV-052), so an
    /// over-consumption here is a programmer error, not a business failure.
    /// </summary>
    public void Consume(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantity, RemainingQuantity);

        RemainingQuantity -= quantity;
    }

    /// <summary>
    /// Apply a signed change to the remaining quantity — the single write path for every Epic 2
    /// operation that moves an <b>existing</b> batch: adjustments in either direction
    /// (DEC-INV-032), write-off, and the two returns (BR-INV-069).
    ///
    /// <para><b>The floor rule lives here, once</b> (BR-INV-061): a change that would drive the
    /// remaining quantity below zero is <b>rejected as a business failure</b> — never clamped to
    /// zero, never applied partially. It is a <see cref="BusinessRuleException"/> rather than an
    /// argument exception precisely because it <i>is</i> a legitimate business outcome the user
    /// can cause: asking to remove more than the batch holds. Keeping the guard on the aggregate
    /// stops C4/C5/C6 from each re-implementing it and drifting.</para>
    ///
    /// <para><see cref="Quantity"/> — the historical received amount — never changes, and a batch
    /// driven to zero simply becomes "depleted" by the existing derivation (BR-INV-021): no new
    /// batch state is introduced (DEC-INV-011/012).</para>
    /// </summary>
    public void ApplyDelta(decimal delta)
    {
        // A zero-quantity operation records nothing and would write an empty ledger row.
        ArgumentOutOfRangeException.ThrowIfEqual(delta, 0m);

        var updated = RemainingQuantity + delta;
        if (updated < 0m)
        {
            throw new BusinessRuleException(
                InventoryErrorCodes.QuantityBelowZero,
                new Dictionary<string, string>
                {
                    ["batchId"] = Id.ToString(),
                    ["remaining"] = RemainingQuantity.ToString(CultureInfo.InvariantCulture),
                    ["requested"] = delta.ToString(CultureInfo.InvariantCulture),
                });
        }

        RemainingQuantity = updated;
    }
}
