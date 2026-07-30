namespace VetFlow.Application.Inventory.Queries.ExpiryMonitoring;

/// <summary>
/// One row of expiry monitoring (REQ-INV-004): an active batch with a real expiry, showing
/// the four frozen fields (BR-INV-034) — the product, the stable batch identifier, the
/// remaining quantity (in the product's stock unit), and the expiry date. A read-only
/// projection; it owns no expiry state (BR-INV-032, DEC-INV-018). Only primitive values cross
/// the boundary — the handler resolves the Catalog reference data (ADR-0014 §2, isolation test).
/// </summary>
public sealed record ExpiryMonitoringItemDto
{
    /// <summary>The product the batch belongs to (kept for the row key; the four displayed fields follow).</summary>
    public required Guid ProductId { get; init; }

    public required string ProductName { get; init; }

    /// <summary>The batch's existing stable identity, shown read-only (BR-INV-025).</summary>
    public required Guid BatchId { get; init; }

    /// <summary>Remaining quantity as stored, in the product's stock unit (BR-INV-034).</summary>
    public required decimal RemainingQuantity { get; init; }

    public required string StockUnitName { get; init; }

    /// <summary>Expiry date — always present in this projection (only batches with an expiry appear).</summary>
    public required DateOnly ExpiryDate { get; init; }
}
