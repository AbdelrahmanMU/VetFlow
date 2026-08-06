using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Queries.InventoryDashboardSummary;

/// <summary>
/// The inventory facts the operational dashboard shows (REQ-INV-013): the expired and
/// expiring-soon batch counts, the out-of-stock product count, and the five most recent
/// stock movements — in <b>one</b> read owned by Inventory.
/// <para>
/// <b>It exists here, and not in the Dashboard, because the Dashboard may not compute it.</b>
/// BR-DSH-001 and DEC-DSH-001 make the dashboard a read-<i>composition</i> module: it owns no
/// business fact and calculates none. That is the same ruling as DEC-INV-040 — rather than
/// let another screen bend BR-INV-014's exclusive filter list or re-derive BR-INV-008 outside
/// its owner, <b>the owning module serves its own read</b>.
/// </para>
/// <para>
/// <b>Nothing is redefined here.</b> Expired and expiring-soon are BR-INV-036 over the
/// BR-INV-033 scope with the approved 30-day horizon (BR-INV-013, DEC-INV-005); out-of-stock
/// is BR-INV-011 over the BR-INV-007 population; the movements are BR-INV-041's fields in
/// BR-INV-044's order. Read-only, owning no state (BR-INV-006).
/// </para>
/// <para>
/// <b>Low stock is deliberately absent.</b> It stays blocked until Catalog introduces a
/// per-product reorder level — DEC-INV-004 forbids inventing a general threshold, and the
/// owner reaffirmed that on 2026-08-03 (DEC-DSH-005, DEC-DSH-013). No placeholder exists
/// anywhere on this path.
/// </para>
/// </summary>
public sealed record InventoryDashboardSummaryQuery : IQuery<InventoryDashboardSummaryDto>
{
    /// <summary>How many recent movements the dashboard shows (BR-DSH-010 — five, fixed).</summary>
    public const int RecentMovementCount = 5;
}
