namespace VetFlow.Application.Inventory.Queries.ProductInventorySummary;

/// <summary>
/// The per-product inventory summary response (REQ-INV-012) — the four inventory facts
/// for one product, in the product's canonical stock unit. Only primitive values cross
/// the boundary (ADR-0014 §2, isolation test).
/// <para>
/// <b>"No stock" is a real answer, not a missing one.</b> A product that exists but has
/// never been received — or has been fully consumed — returns a summary of zero with
/// <see cref="HasInventoryRecord"/> false, so the screen can say «لا يوجد مخزون» rather
/// than print an unexplained 0. A product that does not exist at all yields a null
/// result, which the endpoint turns into 404 (the REQ-INV-003 precedent, AC-INV-022).
/// </para>
/// </summary>
public sealed record ProductInventorySummaryDto
{
    public required Guid ProductId { get; init; }

    /// <summary>On-hand quantity in the product's canonical stock unit (BR-INV-008).</summary>
    public required decimal OnHandQuantity { get; init; }

    /// <summary>The product's canonical stock unit (Catalog <c>StorageUnit</c>, BR-CAT-020).</summary>
    public required string StockUnitName { get; init; }

    /// <summary>Count of active batches — RemainingQuantity &gt; 0 (BR-INV-009).</summary>
    public required int BatchCount { get; init; }

    /// <summary>Nearest expiry across active batches; null when none has one (BR-INV-010).</summary>
    public required DateOnly? NearestExpiry { get; init; }

    /// <summary>
    /// False when the product has no <c>ProductOnHand</c> record at all — it has never been
    /// received (BR-INV-007, DEC-INV-003). Distinguishes "never stocked" from "stocked and
    /// now at zero", which read identically in the numbers alone.
    /// </summary>
    public required bool HasInventoryRecord { get; init; }
}
