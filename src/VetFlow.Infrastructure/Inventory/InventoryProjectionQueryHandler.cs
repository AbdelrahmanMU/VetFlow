using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Queries.InventoryProjection;
using VetFlow.Domain.Catalog;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Inventory;

/// <summary>
/// Inventory projection query implementation (REQ-INV-002). A read-only CQRS-lite
/// projection over the canonical write-kernel state — <see cref="Domain.Inventory.ProductOnHand"/>
/// and <see cref="Domain.Inventory.InventoryBatch"/> — joined to the Catalog reference
/// data (product name, stock unit). The join is the sanctioned cross-module read
/// inside a query handler (ADR-0014 §2); the Inventory module owns no Catalog data.
/// Everything runs as one SQL statement — the batch aggregates are grouped once and
/// left-joined, so filters and sorting act on real columns: no N+1, no per-row
/// lookups (BR-INV-017, DEC-INV-006). The projection owns no state (BR-INV-006).
/// </summary>
public sealed class InventoryProjectionQueryHandler(VetFlowDbContext dbContext, TimeProvider timeProvider)
    : IQueryHandler<InventoryProjectionQuery, PagedResult<InventoryProjectionItemDto>>
{
    private const string LikeEscapeCharacter = "\\";

    public async Task<PagedResult<InventoryProjectionItemDto>> HandleAsync(
        InventoryProjectionQuery query,
        CancellationToken cancellationToken)
    {
        // "Expiring soon" is measured from today; the cutoff is computed in C# from the
        // injected clock and captured, never evaluated inside the translated query.
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var expiringSoonCutoff = today.AddDays(InventoryProjectionQuery.ExpiringSoonHorizonDays);

        var products = ApplyProductFilters(dbContext.Products.AsNoTracking(), query);

        // Only products that have an on-hand record appear (BR-INV-007) — the inner join
        // to ProductOnHand enforces it. The active-batch aggregates (BR-INV-009/010) are
        // correlated scalar subqueries: the count of active batches (RemainingQuantity > 0)
        // and the nearest expiry (SQL MIN ignores NULLs, so no-expiry batches drop out and
        // a product with none yields null). All of it is one SQL statement — no N+1, no
        // per-row lookups (BR-INV-017, DEC-INV-006).
        var rows =
            from onHand in dbContext.ProductOnHands.AsNoTracking()
            join product in products on onHand.ProductId equals product.Id
            join storageUnit in dbContext.Units.AsNoTracking() on product.StorageUnitId equals storageUnit.Id
            select new InventoryProjectionRow
            {
                ProductId = onHand.ProductId,
                ProductName = product.ArabicName,
                OnHandQuantity = onHand.OnHandQuantity,
                StockUnitName = storageUnit.Name,
                BatchCount = dbContext.InventoryBatches
                    .Count(batch => batch.ProductId == onHand.ProductId && batch.RemainingQuantity > 0m),
                NearestExpiry = dbContext.InventoryBatches
                    .Where(batch => batch.ProductId == onHand.ProductId && batch.RemainingQuantity > 0m)
                    .Min(batch => batch.ExpiryDate),
            };

        rows = ApplyRowFilters(rows, query, expiringSoonCutoff);

        var totalCount = await rows.CountAsync(cancellationToken);

        var items = await ApplySorting(rows, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(row => new InventoryProjectionItemDto
            {
                ProductId = row.ProductId,
                ProductName = row.ProductName,
                OnHandQuantity = row.OnHandQuantity,
                StockUnitName = row.StockUnitName,
                BatchCount = row.BatchCount,
                NearestExpiry = row.NearestExpiry,
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<InventoryProjectionItemDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    private static IQueryable<Product> ApplyProductFilters(IQueryable<Product> products, InventoryProjectionQuery query)
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

    private static IQueryable<InventoryProjectionRow> ApplyRowFilters(
        IQueryable<InventoryProjectionRow> rows,
        InventoryProjectionQuery query,
        DateOnly expiringSoonCutoff)
    {
        if (query.OutOfStock == true)
        {
            rows = rows.Where(row => row.OnHandQuantity == 0m);
        }

        if (query.ExpiringSoon == true)
        {
            // Products with a nearest expiry within the 30-day horizon (BR-INV-013);
            // products with no expiry never match.
            rows = rows.Where(row => row.NearestExpiry != null && row.NearestExpiry <= expiringSoonCutoff);
        }

        return rows;
    }

    private static IOrderedQueryable<InventoryProjectionRow> ApplySorting(
        IQueryable<InventoryProjectionRow> rows,
        InventoryProjectionQuery query)
    {
        var ascending = query.Direction == SortDirection.Ascending;

        var ordered = query.Sort switch
        {
            InventoryProjectionSortField.OnHand => ascending
                ? rows.OrderBy(row => row.OnHandQuantity)
                : rows.OrderByDescending(row => row.OnHandQuantity),
            // Products with no expiry sort last in both directions (nulls last).
            InventoryProjectionSortField.NearestExpiry => ascending
                ? rows.OrderBy(row => row.NearestExpiry == null).ThenBy(row => row.NearestExpiry)
                : rows.OrderBy(row => row.NearestExpiry == null).ThenByDescending(row => row.NearestExpiry),
            _ => ascending
                ? rows.OrderBy(row => row.ProductName)
                : rows.OrderByDescending(row => row.ProductName),
        };

        // A unique final key gives offset pagination a total order, so pages stay stable
        // even when the sort key ties (Arabic names may repeat — R3 tiebreaker).
        return ordered.ThenBy(row => row.ProductId);
    }

    private static string EscapeLike(string value) =>
        value.Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);

    private sealed record InventoryProjectionRow
    {
        public required Guid ProductId { get; init; }

        public required string ProductName { get; init; }

        public required decimal OnHandQuantity { get; init; }

        public required string StockUnitName { get; init; }

        public required int BatchCount { get; init; }

        public DateOnly? NearestExpiry { get; init; }
    }
}
