using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Queries.InventoryHistory;
using VetFlow.Domain.Inventory;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Inventory;

/// <summary>
/// Inventory movement history implementation (REQ-INV-005, reopened by DEC-INV-038). A read-only
/// CQRS-lite projection <b>over the movement ledger</b> — one ledger row renders one history row,
/// with nothing derived and no balance computed from it (BR-INV-040 as corrected, BR-INV-063).
///
/// <para>The Catalog product name, the stock unit, and the causing document are resolved inside
/// the single projection statement: the join keys are plain Guids (no cross-module FK, the
/// write-kernel precedent) and only primitives reach the Application DTO (ADR-0014 §2, isolation
/// test). Both document joins are <b>left</b> joins, because an inventory-native movement has no
/// document at all (DEC-INV-036) and must still appear.</para>
///
/// <para><b>Query count is constant</b>: one projection SELECT plus the standard pagination COUNT,
/// regardless of how many rows or how many distinct movement types a page contains — no per-row
/// lookups, no N+1, no lazy loading (BR-INV-045).</para>
/// </summary>
public sealed class InventoryHistoryQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<InventoryHistoryQuery, PagedResult<InventoryHistoryItemDto>>
{
    public async Task<PagedResult<InventoryHistoryItemDto>> HandleAsync(
        InventoryHistoryQuery query,
        CancellationToken cancellationToken)
    {
        // Every movement, every type, every source — the history is clinic-wide and unfiltered in
        // this slice (BR-INV-044). A movement always has a product and a batch, so those two joins
        // are inner; the document joins below are not.
        var rows =
            from movement in dbContext.InventoryMovements.AsNoTracking()
            join product in dbContext.Products.AsNoTracking() on movement.ProductId equals product.Id
            join stockUnit in dbContext.Units.AsNoTracking() on product.StorageUnitId equals stockUnit.Id

            // Receive carries the purchase line; Consume carries the sale line (BR-INV-057). Each
            // resolves to its invoice for the reference label and the navigation target
            // (BR-INV-043). A movement matches at most one of the two.
            join purchaseLine in dbContext.PurchaseLineItems.AsNoTracking()
                on movement.ReferenceId equals purchaseLine.Id into purchaseLines
            from purchaseLine in purchaseLines.DefaultIfEmpty()
            join purchaseInvoice in dbContext.PurchaseInvoices.AsNoTracking()
                on EF.Property<Guid>(purchaseLine, "PurchaseInvoiceId") equals purchaseInvoice.Id into purchaseInvoices
            from purchaseInvoice in purchaseInvoices.DefaultIfEmpty()

            join saleLine in dbContext.SalesLineItems.AsNoTracking()
                on movement.ReferenceId equals saleLine.Id into saleLines
            from saleLine in saleLines.DefaultIfEmpty()
            join saleInvoice in dbContext.SalesInvoices.AsNoTracking()
                on EF.Property<Guid>(saleLine, "SalesInvoiceId") equals saleInvoice.Id into saleInvoices
            from saleInvoice in saleInvoices.DefaultIfEmpty()

            select new
            {
                movement.Id,
                movement.OccurredAt,
                movement.Type,
                movement.BatchId,
                movement.Quantity,
                movement.Source,
                ProductName = product.ArabicName,
                StockUnitName = stockUnit.Name,
                PurchaseNumber = purchaseInvoice != null ? purchaseInvoice.Number : null,
                PurchaseInvoiceId = purchaseInvoice != null ? (Guid?)purchaseInvoice.Id : null,
                SalesNumber = saleInvoice != null ? saleInvoice.Number : null,
                SalesInvoiceId = saleInvoice != null ? (Guid?)saleInvoice.Id : null,
            };

        var totalCount = await rows.CountAsync(cancellationToken);

        // Newest first — the activity-log convention BR-INV-044 chose deliberately — tie-broken by
        // the movement's stable identity, which gives offset pagination a total order so no row is
        // repeated or lost across pages.
        var items = await rows
            .OrderByDescending(row => row.OccurredAt)
            .ThenBy(row => row.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryHistoryItemDto>
        {
            Items = items.Select(row =>
            {
                // Which document a row points at follows from the movement type, not from whichever
                // join happened to match: only Receive resolves a purchase invoice and only Consume
                // a sales invoice. Everything else is inventory-native and shows "—" (BR-INV-043).
                var (label, target, referenceId) = row.Type switch
                {
                    InventoryMovementType.Receive when row.PurchaseInvoiceId is not null =>
                        (row.PurchaseNumber, MovementReferenceTargetDto.PurchaseInvoice, row.PurchaseInvoiceId),
                    InventoryMovementType.Consume when row.SalesInvoiceId is not null =>
                        (row.SalesNumber, MovementReferenceTargetDto.SalesInvoice, row.SalesInvoiceId),
                    _ => (null, MovementReferenceTargetDto.None, (Guid?)null),
                };

                return new InventoryHistoryItemDto
                {
                    MovementId = row.Id,
                    OccurredAt = row.OccurredAt,
                    Type = (InventoryMovementTypeDto)row.Type,
                    ProductName = row.ProductName,
                    BatchId = row.BatchId,
                    Quantity = row.Quantity,
                    StockUnitName = row.StockUnitName,
                    ReferenceLabel = label,
                    ReferenceTarget = target,
                    ReferenceId = referenceId,
                    Source = (InventoryMovementSourceDto)row.Source,
                };
            }).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }
}
