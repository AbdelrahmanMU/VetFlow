using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Queries.ExpiryMonitoring;
using VetFlow.Domain.Catalog;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Inventory;

/// <summary>
/// Expiry monitoring query implementation (REQ-INV-004). A read-only CQRS-lite projection over
/// the write-kernel <see cref="Domain.Inventory.InventoryBatch"/> rows, clinic-wide, joined to
/// the Catalog product (name, category, search) and stock unit. Scope: active batches with a
/// real expiry only (BR-INV-033, DEC-INV-014). "Expired"/"expiring soon" are WHERE clauses on
/// <c>ExpiryDate</c>, evaluated against the injected clock captured in C# — the projection owns
/// no expiry state; nothing is cached, materialized, or refreshed (DEC-INV-018). Everything is
/// one projection SELECT plus the standard pagination COUNT — no per-row lookups, no N+1, no
/// lazy loading (BR-INV-038). The order is deterministic (BR-INV-037).
/// </summary>
public sealed class ExpiryMonitoringQueryHandler(VetFlowDbContext dbContext, IClinicClock clinicClock)
    : IQueryHandler<ExpiryMonitoringQuery, PagedResult<ExpiryMonitoringItemDto>>
{
    private const string LikeEscapeCharacter = "\\";

    public async Task<PagedResult<ExpiryMonitoringItemDto>> HandleAsync(
        ExpiryMonitoringQuery query,
        CancellationToken cancellationToken)
    {
        // "Expired"/"expiring soon" are measured from the <b>clinic local date</b> — never UTC,
        // never server or device time (BR-INV-059/060, AC-INV-049) — computed in C# and captured,
        // never evaluated inside the translated query. Expiry is derived here, never stored
        // (DEC-INV-018).
        var today = clinicClock.Today;
        var expiringSoonCutoff = today.AddDays(ExpiryMonitoringQuery.ExpiringSoonHorizonDays);

        var products = ApplyProductFilters(dbContext.Products.AsNoTracking(), query);

        // Scope (DEC-INV-014): active batches (RemainingQuantity > 0) with a real expiry only —
        // a batch with no expiry, or a depleted one, never appears. One SQL statement joins the
        // product (name/search/category) and the stock unit; no per-row lookups (BR-INV-038).
        // Filtering/sorting act on an intermediate row, then the DTO is projected last — the
        // proven inventory-projection shape.
        var rows =
            from batch in dbContext.InventoryBatches.AsNoTracking()
            where batch.RemainingQuantity > 0m && batch.ExpiryDate != null
            join product in products on batch.ProductId equals product.Id
            join stockUnit in dbContext.Units.AsNoTracking() on product.StorageUnitId equals stockUnit.Id
            select new ExpiryMonitoringRow
            {
                ProductId = batch.ProductId,
                ProductName = product.ArabicName,
                BatchId = batch.Id,
                RemainingQuantity = batch.RemainingQuantity,
                StockUnitName = stockUnit.Name,
                ExpiryDate = batch.ExpiryDate!.Value,
            };

        rows = ApplyExpiryFilters(rows, query, today, expiringSoonCutoff);

        var totalCount = await rows.CountAsync(cancellationToken);

        // Deterministic order: expiry date ascending (most urgent first), tie-broken by the
        // stable batch identifier ascending — a total order for stable pagination (BR-INV-037).
        var items = await rows
            .OrderBy(row => row.ExpiryDate)
            .ThenBy(row => row.BatchId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(row => new ExpiryMonitoringItemDto
            {
                ProductId = row.ProductId,
                ProductName = row.ProductName,
                BatchId = row.BatchId,
                RemainingQuantity = row.RemainingQuantity,
                StockUnitName = row.StockUnitName,
                ExpiryDate = row.ExpiryDate,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ExpiryMonitoringItemDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    private static IQueryable<Product> ApplyProductFilters(IQueryable<Product> products, ExpiryMonitoringQuery query)
    {
        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(product => product.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(ArabicSearchText.Normalize(query.Search.Trim()))}%";
            products = products.Where(product =>
                EF.Functions.ILike(EF.Property<string>(product, SearchableText.PropertyName), pattern, LikeEscapeCharacter));
        }

        return products;
    }

    private static IQueryable<ExpiryMonitoringRow> ApplyExpiryFilters(
        IQueryable<ExpiryMonitoringRow> rows,
        ExpiryMonitoringQuery query,
        DateOnly today,
        DateOnly expiringSoonCutoff)
    {
        if (query.Expired == true)
        {
            rows = rows.Where(row => row.ExpiryDate < today);
        }

        if (query.ExpiringSoon == true)
        {
            rows = rows.Where(row => row.ExpiryDate >= today && row.ExpiryDate <= expiringSoonCutoff);
        }

        return rows;
    }

    private static string EscapeLike(string value) =>
        value.Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);

    private sealed record ExpiryMonitoringRow
    {
        public required Guid ProductId { get; init; }

        public required string ProductName { get; init; }

        public required Guid BatchId { get; init; }

        public required decimal RemainingQuantity { get; init; }

        public required string StockUnitName { get; init; }

        public required DateOnly ExpiryDate { get; init; }
    }
}
