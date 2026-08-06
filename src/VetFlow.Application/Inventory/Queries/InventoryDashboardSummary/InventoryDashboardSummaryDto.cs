using VetFlow.Application.Inventory.Queries.InventoryHistory;

namespace VetFlow.Application.Inventory.Queries.InventoryDashboardSummary;

/// <summary>
/// Inventory's answer to the dashboard (REQ-INV-013, BR-INV-070). Only primitives and this
/// module's own enums cross the boundary (ADR-0014 §2, isolation test).
/// <para>
/// <b>The counting unit differs per fact, on purpose.</b> Expiry is a property of a
/// <i>batch</i> (BR-INV-001) and the expiry screen shows one row per batch (BR-INV-034), so
/// the expiry counts are batch counts. Stock level is a property of a <i>product</i> and the
/// inventory projection shows one row per product (BR-INV-007), so out-of-stock is a product
/// count. <b>Each number therefore equals the row count of the screen it links to</b> — which
/// AC-INV-069 pins by test rather than by comment.
/// </para>
/// </summary>
public sealed record InventoryDashboardSummaryDto
{
    /// <summary>
    /// Active batches whose expiry has passed — BR-INV-036 (<c>ExpiryDate &lt; clinic local
    /// date</c>) over the BR-INV-033 scope. A batch expiring <i>today</i> is <b>not</b> here:
    /// the expiry date is the last saleable day (BR-INV-059).
    /// </summary>
    public required int ExpiredBatchCount { get; init; }

    /// <summary>
    /// Active batches expiring within the approved 30-day horizon — BR-INV-036, reusing
    /// BR-INV-013 / DEC-INV-005. Disjoint from <see cref="ExpiredBatchCount"/> by definition,
    /// so no batch is counted twice.
    /// </summary>
    public required int ExpiringSoonBatchCount { get; init; }

    /// <summary>
    /// Products whose on-hand balance is zero — BR-INV-011 over the BR-INV-007 / DEC-INV-003
    /// population. <b>A product never received is not here</b>: it has no
    /// <c>ProductOnHand</c> row at all, and it is absent from the destination screen for the
    /// same reason.
    /// </summary>
    public required int OutOfStockProductCount { get; init; }

    /// <summary>
    /// The five most recent movements (BR-DSH-010) in the ledger's deterministic order
    /// (BR-INV-044). Fewer than five is returned as-is and never padded; an empty ledger is an
    /// empty list, not an error.
    /// </summary>
    public required IReadOnlyList<InventoryDashboardMovementDto> RecentMovements { get; init; }
}

/// <summary>
/// One movement as the dashboard shows it — <b>four fields</b> (BR-DSH-010): when, what kind,
/// which product, how much.
/// <para>
/// <b>Deliberately narrower than <see cref="InventoryHistoryItemDto"/>.</b> The history screen
/// owns BR-INV-041's seven frozen fields; the dashboard is a glance that leads to that screen,
/// not a smaller copy of it (BR-DSH-010). Carrying the batch id, reference and source here
/// would make the dashboard compete with the destination it exists to hand off to.
/// </para>
/// </summary>
public sealed record InventoryDashboardMovementDto
{
    public required Guid MovementId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>The movement type, from the closed set (BR-INV-065).</summary>
    public required InventoryMovementTypeDto Type { get; init; }

    /// <summary>Arabic product name — a Catalog display snapshot.</summary>
    public required string ProductName { get; init; }

    /// <summary>Signed quantity in the product's stock unit; never rounded (BR-INV-058, BR-INV-064).</summary>
    public required decimal Quantity { get; init; }

    public required string StockUnitName { get; init; }
}
