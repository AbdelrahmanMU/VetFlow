using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Queries.ProductInventorySummary;

/// <summary>
/// The per-product inventory summary (REQ-INV-012): the same four inventory facts the
/// inventory projection reports for one product — current on-hand, its stock unit, the
/// active-batch count and the nearest expiry — scoped to a single product so a screen
/// outside Inventory can show them <b>without</b> reading Inventory's tables, paging its
/// batch list, or re-deriving anything.
/// <para>
/// It exists because the Catalog product-details screen (catalog ui.md §4, card 7) must
/// answer "how much do I have?" beside "how much is a box?". The inventory projection
/// (REQ-INV-002) cannot serve it: <b>BR-INV-014 declares that screen's filter list
/// exclusive</b>, so it may not gain a product filter. Rather than bend that rule, the
/// owning module exposes its own scoped read — the same choice REQ-INV-003 made for
/// per-product batches.
/// </para>
/// <para>
/// The facts are <b>not redefined here</b>: on-hand is the canonical stored
/// <c>ProductOnHand.OnHandQuantity</c> (BR-INV-008), the batch count is active batches
/// only (BR-INV-009), and the nearest expiry is the minimum expiry across those same
/// active batches (BR-INV-010). A read-only projection; it owns no inventory state
/// (BR-INV-006).
/// </para>
/// </summary>
public sealed record ProductInventorySummaryQuery : IQuery<ProductInventorySummaryDto?>
{
    /// <summary>The product whose inventory is summarised — the query's whole scope.</summary>
    public required Guid ProductId { get; init; }
}
