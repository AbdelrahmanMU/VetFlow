namespace VetFlow.Application.Inventory.Queries.BatchViewer;

/// <summary>
/// One row of the batch viewer (REQ-INV-003): a single inventory batch of the product,
/// with the nine frozen fields (BR-INV-020) — the stable batch identifier, the purchase
/// reference (the owning invoice number + id, a navigation link — BR-INV-024, DEC-INV-010),
/// the receive date, the original and remaining quantities, the stock-unit name, the
/// unit-cost snapshot, the expiry date (null when none), and the derived status
/// (BR-INV-021). A read-only projection; it owns no inventory state (BR-INV-018). Only
/// primitive values cross the boundary — the handler resolves the Catalog/Purchasing
/// reference data (ADR-0014 §2, isolation test).
/// </summary>
public sealed record BatchViewerItemDto
{
    /// <summary>The batch's existing stable identity, shown read-only — no new field (BR-INV-025, DEC-INV-009).</summary>
    public required Guid BatchId { get; init; }

    /// <summary>The owning purchase invoice number, e.g. PUR-000001 (BR-INV-024).</summary>
    public required string PurchaseReference { get; init; }

    /// <summary>The owning purchase invoice id — the navigation target /purchases/:id (BR-INV-024, DEC-INV-010).</summary>
    public required Guid PurchaseInvoiceId { get; init; }

    /// <summary>Receive timestamp (BR-INV-020).</summary>
    public required DateTimeOffset ReceiveDate { get; init; }

    /// <summary>Original received quantity in the product's stock unit — immutable (BR-INV-022).</summary>
    public required decimal OriginalQuantity { get; init; }

    /// <summary>Remaining quantity as stored, in the product's stock unit (BR-INV-022).</summary>
    public required decimal RemainingQuantity { get; init; }

    public required string StockUnitName { get; init; }

    /// <summary>Unit-cost snapshot — a frozen historical value in the system currency (BR-INV-022).</summary>
    public required decimal UnitCostSnapshot { get; init; }

    /// <summary>Expiry date; null when the batch has none (BR-INV-023).</summary>
    public DateOnly? ExpiryDate { get; init; }

    /// <summary>Derived Active/Depleted status; never stored (BR-INV-021).</summary>
    public required BatchStatus Status { get; init; }
}
