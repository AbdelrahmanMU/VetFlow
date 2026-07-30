using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Queries.BatchViewer;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Inventory;

/// <summary>
/// Batch viewer query implementation (REQ-INV-003). A read-only CQRS-lite projection over
/// the write-kernel <see cref="Domain.Inventory.InventoryBatch"/> rows of a single product,
/// joined to the Catalog stock unit and to the owning Purchasing invoice for the purchase
/// reference. The cross-module reads are the sanctioned join inside a query handler
/// (ADR-0014 §2) — the join keys are plain Guids (no cross-module FK/navigation, the
/// write-kernel precedent), and only primitive values reach the Application DTO. The batch
/// list is one projection SELECT plus the standard pagination COUNT — no per-row lookups,
/// no N+1, no lazy loading (BR-INV-030). A single O(1) product-existence guard resolves the
/// header and distinguishes "not found" from "empty" (AC-INV-022). The projection owns no
/// state (BR-INV-018).
/// </summary>
public sealed class BatchViewerQueryHandler(VetFlowDbContext dbContext, IClinicClock clinicClock)
    : IQueryHandler<BatchViewerQuery, BatchViewerResult?>
{
    public async Task<BatchViewerResult?> HandleAsync(
        BatchViewerQuery query,
        CancellationToken cancellationToken)
    {
        // "Expired"/"expiring soon" are measured from the <b>clinic local date</b> — never UTC,
        // never server or device time (BR-INV-059/060, AC-INV-049) — computed in C# and captured,
        // never evaluated inside the translated query.
        var today = clinicClock.Today;
        var expiringSoonCutoff = today.AddDays(BatchViewerQuery.ExpiringSoonHorizonDays);

        // One product-existence guard: resolves the header (name + stock unit) and separates
        // "not found" (null → 404) from "empty" (a valid product with no batches). This is an
        // O(1) lookup, not a per-row one — the batch list below stays a single projection query.
        var header = await (
            from product in dbContext.Products.AsNoTracking()
            where product.Id == query.ProductId
            join stockUnit in dbContext.Units.AsNoTracking() on product.StorageUnitId equals stockUnit.Id
            select new { product.ArabicName, StockUnitName = stockUnit.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (header is null)
        {
            return null;
        }

        // All batches of the product (active and depleted — BR-INV-019). The purchase reference
        // resolves through the two-hop join InventoryBatch.PurchaseLineId → PurchaseLineItem →
        // (shadow FK) PurchaseInvoice, all on plain Guids in one statement (BR-INV-024, BR-INV-030).
        var rows =
            from batch in dbContext.InventoryBatches.AsNoTracking()
            where batch.ProductId == query.ProductId
            join line in dbContext.PurchaseLineItems.AsNoTracking() on batch.PurchaseLineId equals line.Id
            join invoice in dbContext.PurchaseInvoices.AsNoTracking()
                on EF.Property<Guid>(line, "PurchaseInvoiceId") equals invoice.Id
            select new BatchViewerRow
            {
                BatchId = batch.Id,
                PurchaseReference = invoice.Number,
                PurchaseInvoiceId = invoice.Id,
                ReceiveDate = batch.ReceivedAt,
                OriginalQuantity = batch.Quantity,
                RemainingQuantity = batch.RemainingQuantity,
                UnitCostSnapshot = batch.UnitCostSnapshot,
                ExpiryDate = batch.ExpiryDate,
                IsActive = batch.RemainingQuantity > 0m,
            };

        rows = ApplyFilters(rows, query, today, expiringSoonCutoff);

        var totalCount = await rows.CountAsync(cancellationToken);

        var items = await ApplySorting(rows, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(row => new BatchViewerItemDto
            {
                BatchId = row.BatchId,
                PurchaseReference = row.PurchaseReference,
                PurchaseInvoiceId = row.PurchaseInvoiceId,
                ReceiveDate = row.ReceiveDate,
                OriginalQuantity = row.OriginalQuantity,
                RemainingQuantity = row.RemainingQuantity,
                StockUnitName = header.StockUnitName,
                UnitCostSnapshot = row.UnitCostSnapshot,
                ExpiryDate = row.ExpiryDate,
                Status = row.IsActive ? BatchStatus.Active : BatchStatus.Depleted,
            })
            .ToListAsync(cancellationToken);

        return new BatchViewerResult
        {
            ProductName = header.ArabicName,
            StockUnitName = header.StockUnitName,
            Batches = new PagedResult<BatchViewerItemDto>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            },
        };
    }

    private static IQueryable<BatchViewerRow> ApplyFilters(
        IQueryable<BatchViewerRow> rows,
        BatchViewerQuery query,
        DateOnly today,
        DateOnly expiringSoonCutoff)
    {
        if (query.Status is { } status)
        {
            var active = status == BatchStatus.Active;
            rows = rows.Where(row => row.IsActive == active);
        }

        // Expired/expiring-soon are batch-level derived filters, not statuses (DEC-INV-012);
        // a batch with no expiry never matches either.
        if (query.Expired == true)
        {
            rows = rows.Where(row => row.ExpiryDate != null && row.ExpiryDate < today);
        }

        if (query.ExpiringSoon == true)
        {
            rows = rows.Where(row =>
                row.ExpiryDate != null && row.ExpiryDate >= today && row.ExpiryDate <= expiringSoonCutoff);
        }

        return rows;
    }

    private static IOrderedQueryable<BatchViewerRow> ApplySorting(
        IQueryable<BatchViewerRow> rows,
        BatchViewerQuery query)
    {
        var ascending = query.Direction == SortDirection.Ascending;

        var ordered = query.Sort switch
        {
            // Batches with no expiry sort last in both directions (nulls last).
            BatchViewerSortField.ExpiryDate => ascending
                ? rows.OrderBy(row => row.ExpiryDate == null).ThenBy(row => row.ExpiryDate)
                : rows.OrderBy(row => row.ExpiryDate == null).ThenByDescending(row => row.ExpiryDate),
            BatchViewerSortField.RemainingQuantity => ascending
                ? rows.OrderBy(row => row.RemainingQuantity)
                : rows.OrderByDescending(row => row.RemainingQuantity),
            _ => ascending
                ? rows.OrderBy(row => row.ReceiveDate)
                : rows.OrderByDescending(row => row.ReceiveDate),
        };

        // The stable batch identifier is the unique final key — it gives offset pagination a
        // total order so pages stay stable, and it is the default order's tie-breaker (BR-INV-031).
        return ordered.ThenBy(row => row.BatchId);
    }

    private sealed record BatchViewerRow
    {
        public required Guid BatchId { get; init; }

        public required string PurchaseReference { get; init; }

        public required Guid PurchaseInvoiceId { get; init; }

        public required DateTimeOffset ReceiveDate { get; init; }

        public required decimal OriginalQuantity { get; init; }

        public required decimal RemainingQuantity { get; init; }

        public required decimal UnitCostSnapshot { get; init; }

        public DateOnly? ExpiryDate { get; init; }

        public required bool IsActive { get; init; }
    }
}
