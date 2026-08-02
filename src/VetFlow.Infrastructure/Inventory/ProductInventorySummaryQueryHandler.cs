using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Queries.ProductInventorySummary;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Inventory;

/// <summary>
/// Per-product inventory summary implementation (REQ-INV-012). A read-only CQRS-lite
/// projection over the canonical write-kernel state — <see cref="Domain.Inventory.ProductOnHand"/>
/// and <see cref="Domain.Inventory.InventoryBatch"/> — joined to the Catalog stock unit.
/// The join is the sanctioned cross-module read inside a query handler (ADR-0014 §2) on
/// plain Guids; the Inventory module owns no Catalog data, and only primitives reach the DTO.
/// The projection owns no state (BR-INV-006).
/// <para>
/// <b>The three facts are read exactly as <see cref="InventoryProjectionQueryHandler"/>
/// reads them</b>, from the same columns and with the same predicates: on-hand is the
/// stored canonical value (BR-INV-008) — never a sum computed here — while the batch count
/// and nearest expiry are correlated scalar subqueries over active batches
/// (RemainingQuantity &gt; 0, BR-INV-009/010). SQL <c>MIN</c> ignores NULLs, so batches
/// without an expiry drop out and a product with none yields null.
/// </para>
/// <para>
/// EF Core cannot translate a shared helper method inside an expression tree, so the two
/// handlers state the predicates rather than share a function. <b>The guard against drift
/// is a test, not a comment:</b> an integration test asserts this summary equals the row
/// the inventory projection reports for the same product.
/// </para>
/// <para>
/// One statement, no N+1, no per-row lookups (BR-INV-017, DEC-INV-006). A product with no
/// on-hand record is a valid answer of zero, not a 404 — only a product that does not exist
/// returns null (the REQ-INV-003 precedent, AC-INV-022).
/// </para>
/// </summary>
public sealed class ProductInventorySummaryQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<ProductInventorySummaryQuery, ProductInventorySummaryDto?>
{
    public async Task<ProductInventorySummaryDto?> HandleAsync(
        ProductInventorySummaryQuery query,
        CancellationToken cancellationToken)
    {
        // The product-existence guard also resolves the stock unit, so "not found" and
        // "found but never received" are separated by one O(1) lookup rather than two.
        var summary = await (
            from product in dbContext.Products.AsNoTracking()
            where product.Id == query.ProductId
            join stockUnit in dbContext.Units.AsNoTracking() on product.StorageUnitId equals stockUnit.Id
            select new ProductInventorySummaryDto
            {
                ProductId = product.Id,
                StockUnitName = stockUnit.Name,

                // The canonical stored balance (BR-INV-008). Absent record ⇒ never received
                // (BR-INV-007, DEC-INV-003) ⇒ zero, flagged below so the screen can say so.
                OnHandQuantity = dbContext.ProductOnHands
                    .Where(onHand => onHand.ProductId == product.Id)
                    .Select(onHand => (decimal?)onHand.OnHandQuantity)
                    .FirstOrDefault() ?? 0m,

                HasInventoryRecord = dbContext.ProductOnHands
                    .Any(onHand => onHand.ProductId == product.Id),

                BatchCount = dbContext.InventoryBatches
                    .Count(batch => batch.ProductId == product.Id && batch.RemainingQuantity > 0m),

                NearestExpiry = dbContext.InventoryBatches
                    .Where(batch => batch.ProductId == product.Id && batch.RemainingQuantity > 0m)
                    .Min(batch => batch.ExpiryDate),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return summary;
    }
}
