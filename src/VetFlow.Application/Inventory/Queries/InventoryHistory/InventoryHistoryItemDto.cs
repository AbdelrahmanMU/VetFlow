namespace VetFlow.Application.Inventory.Queries.InventoryHistory;

/// <summary>
/// One row of the inventory movement history (REQ-INV-005) — the <b>seven frozen fields</b> of
/// BR-INV-041 and nothing else: date, movement type, product, batch, quantity, reference, source
/// module.
///
/// <para><b>The ledger carries more than this DTO exposes, deliberately.</b> Reason, reason note
/// and actor name exist on <see cref="Domain.Inventory.InventoryMovement"/> (BR-INV-066/067), but
/// BR-INV-041 locks the screen's field list with "exclusively", and DEC-INV-038 reopened this
/// design for <i>movement types</i> — not for new columns. Surfacing them is the owner's call and
/// has not been made.</para>
///
/// <para>Only primitive values cross the boundary; the handler resolves the Catalog and
/// Purchasing/Sales reference data (ADR-0014 §2, isolation test).</para>
/// </summary>
public sealed record InventoryHistoryItemDto
{
    /// <summary>The movement's stable identity — also the pagination tie-break (BR-INV-044).</summary>
    public required Guid MovementId { get; init; }

    /// <summary>When the movement occurred (BR-INV-041 field 1).</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>The movement type, from the closed set (BR-INV-065, BR-INV-042 as amended).</summary>
    public required InventoryMovementTypeDto Type { get; init; }

    /// <summary>Arabic product name — a Catalog display snapshot (BR-INV-041 field 3).</summary>
    public required string ProductName { get; init; }

    /// <summary>The batch's existing stable identity, shown read-only (BR-INV-025).</summary>
    public required Guid BatchId { get; init; }

    /// <summary>
    /// Signed movement quantity in the product's stock unit — positive increases stock, negative
    /// decreases it (BR-INV-064). Never rounded (BR-INV-058); the sign is shown, not stripped.
    /// </summary>
    public required decimal Quantity { get; init; }

    public required string StockUnitName { get; init; }

    /// <summary>
    /// The causing document's number — a purchase invoice number for a receive, a sales invoice
    /// number for a consumption. <b>Null for inventory-native operations</b>, which have no
    /// counterparty document (DEC-INV-036) and render as "—" (BR-INV-043).
    /// </summary>
    public string? ReferenceLabel { get; init; }

    /// <summary>What <see cref="ReferenceLabel"/> navigates to; <c>None</c> when there is nothing to open.</summary>
    public required MovementReferenceTargetDto ReferenceTarget { get; init; }

    /// <summary>The navigation target's id — the invoice id, not the line id. Null when there is no document.</summary>
    public Guid? ReferenceId { get; init; }

    /// <summary>The module that caused the movement (BR-INV-043, DEC-INV-016).</summary>
    public required InventoryMovementSourceDto Source { get; init; }
}
