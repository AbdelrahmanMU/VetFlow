using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Queries.InventoryDashboardSummary;
using VetFlow.Application.Inventory.Queries.InventoryHistory;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Inventory;

/// <summary>
/// Inventory's dashboard read (REQ-INV-013, BR-INV-070). A read-only CQRS-lite projection over
/// the canonical write-kernel state; it owns nothing and stores nothing (BR-INV-006), and no
/// expiry state is cached, materialized or refreshed (DEC-INV-018, BR-INV-032).
/// <para>
/// <b>The predicates are stated here rather than shared, for the reason DEC-INV-040 already
/// recorded:</b> EF Core cannot translate a shared helper inside an expression tree, so the
/// same conditions necessarily exist in this handler and in the screen handlers. <b>The guard
/// against drift is a test, not a comment</b> — AC-INV-069 asserts each count equals the row
/// count of the screen it links to, on the same data. That is the AC-INV-065 pattern.
/// </para>
/// <para>
/// Three scalar aggregates plus one small ordered read, each a single statement — no per-row
/// lookups, no N+1, no lazy loading (BR-INV-017/030/038). Committed state only (BR-INV-016).
/// </para>
/// </summary>
public sealed class InventoryDashboardSummaryQueryHandler(
    VetFlowDbContext dbContext,
    IClinicClock clinicClock)
    : IQueryHandler<InventoryDashboardSummaryQuery, InventoryDashboardSummaryDto>
{
    public async Task<InventoryDashboardSummaryDto> HandleAsync(
        InventoryDashboardSummaryQuery query,
        CancellationToken cancellationToken)
    {
        // Measured from the clinic local date — never UTC, never server or device time
        // (clinic-date.md, BR-INV-059/060). Computed in C# and captured, never evaluated
        // inside the translated query.
        var today = clinicClock.Today;
        var expiringSoonCutoff = today.AddDays(ExpiryHorizonDays);

        // Scope (BR-INV-033): active batches with a real expiry only. A batch with no expiry
        // has nothing to monitor; a depleted one has nothing to lose.
        var monitoredBatches = dbContext.InventoryBatches
            .AsNoTracking()
            .Where(batch => batch.RemainingQuantity > 0m && batch.ExpiryDate != null);

        // BR-INV-036: expired is strictly before today, because ExpiryDate is the last
        // saleable day (BR-INV-059). A batch expiring today is expiring-soon, not expired —
        // and the two sets are disjoint by construction, so nothing is counted twice.
        var expiredBatchCount = await monitoredBatches
            .CountAsync(batch => batch.ExpiryDate!.Value < today, cancellationToken);

        var expiringSoonBatchCount = await monitoredBatches
            .CountAsync(
                batch => batch.ExpiryDate!.Value >= today && batch.ExpiryDate!.Value <= expiringSoonCutoff,
                cancellationToken);

        // BR-INV-011 over the BR-INV-007 / DEC-INV-003 population: only products that have a
        // ProductOnHand row — i.e. that were received at least once. A product never received
        // is absent here and absent from the destination screen, which is why the tile is
        // labelled "out of stock" and not "cannot be sold" (BR-DSH-006).
        var outOfStockProductCount = await dbContext.ProductOnHands
            .AsNoTracking()
            .CountAsync(onHand => onHand.OnHandQuantity == 0m, cancellationToken);

        // The ledger's own deterministic order, reused exactly (BR-INV-044): newest first,
        // tie-broken by the movement's stable identity ascending — the same two keys the
        // history screen orders by, so "the latest five" means the same thing in both places.
        // Then the fixed five (BR-DSH-010). Fewer than five returns what exists and is never
        // padded; an empty ledger returns an empty list, not an error.
        var recentMovements = await (
            from movement in dbContext.InventoryMovements.AsNoTracking()
            join product in dbContext.Products.AsNoTracking() on movement.ProductId equals product.Id
            join stockUnit in dbContext.Units.AsNoTracking() on product.StorageUnitId equals stockUnit.Id
            orderby movement.OccurredAt descending, movement.Id
            select new InventoryDashboardMovementDto
            {
                MovementId = movement.Id,
                OccurredAt = movement.OccurredAt,

                // The same direct cast the history handler uses: both enums are int-backed and
                // share their values, and Inventory owns the vocabulary either way (BR-INV-065).
                Type = (InventoryMovementTypeDto)movement.Type,
                ProductName = product.ArabicName,
                Quantity = movement.Quantity,
                StockUnitName = stockUnit.Name,
            })
            .Take(InventoryDashboardSummaryQuery.RecentMovementCount)
            .ToListAsync(cancellationToken);

        return new InventoryDashboardSummaryDto
        {
            ExpiredBatchCount = expiredBatchCount,
            ExpiringSoonBatchCount = expiringSoonBatchCount,
            OutOfStockProductCount = outOfStockProductCount,
            RecentMovements = recentMovements,
        };
    }

    /// <summary>
    /// The approved horizon (BR-INV-013, DEC-INV-005) — 30 days, reused, never re-chosen and
    /// deliberately not configurable (BR-INV-013 puts that out of scope).
    /// </summary>
    private const int ExpiryHorizonDays = 30;
}
