namespace VetFlow.Application.Inventory.Queries.InventoryProjection;

/// <summary>
/// One row of the inventory projection (REQ-INV-002): a product that has an
/// on-hand record, with the five frozen columns — the product name, the current
/// on-hand quantity in the product's canonical stock unit, the stock-unit name,
/// the active-batch count, and the nearest expiry (null when none). A read-only
/// projection over data produced by Purchase Receiving; it owns no inventory
/// state (BR-INV-006). Only primitive values cross the boundary — the query
/// handler resolves the Catalog reference data (ADR-0014 §2, isolation test).
/// </summary>
public sealed record InventoryProjectionItemDto
{
    /// <summary>The product this on-hand row belongs to — the row's navigation target (BR-INV-015).</summary>
    public required Guid ProductId { get; init; }

    public required string ProductName { get; init; }

    /// <summary>On-hand quantity in the product's canonical stock unit (BR-INV-008).</summary>
    public required decimal OnHandQuantity { get; init; }

    public required string StockUnitName { get; init; }

    /// <summary>Count of active batches — RemainingQuantity &gt; 0 (BR-INV-009).</summary>
    public required int BatchCount { get; init; }

    /// <summary>Nearest expiry across active batches; null when none has an expiry (BR-INV-010).</summary>
    public DateOnly? NearestExpiry { get; init; }
}
