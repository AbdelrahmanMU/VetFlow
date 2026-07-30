using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Inventory;
using VetFlow.Domain.Common;
using VetFlow.Domain.Inventory;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Inventory;

/// <summary>
/// The Inventory consumption path (REQ-INV-006) and its FEFO allocation (REQ-INV-007) — the second
/// and last inventory movement (BR-INV-055), and the mirror of
/// <see cref="InventoryReceiptWriter"/>. It implements the public contract Sales depends on: Sales
/// states intent, Inventory decides which batches and whether they suffice (DEC-INV-019,
/// BR-SAL-013).
///
/// Sequence (inventory workflow.md):
/// <list type="number">
/// <item>Validate every request and aggregate the quantities per product, <b>without</b> losing
/// each quantity's attribution to its own sale line (BR-INV-046, BR-INV-057).</item>
/// <item>Read the <b>saleable</b> batches of all requested products in <b>one ordered query</b>
/// (BR-INV-053): active (<c>RemainingQuantity &gt; 0</c>) and not expired. Expired stock is
/// excluded <b>in the query, before allocation begins</b> — it never enters the candidate set
/// (DEC-INV-021, BR-INV-050), and expiry is measured against the <b>clinic local date</b>, never
/// UTC, with <c>ExpiryDate</c> being the last saleable day (BR-INV-059/060).</item>
/// <item>Check sufficiency per product against the saleable remaining quantities only. If any
/// product falls short, reject the <b>whole</b> operation and stage nothing — not even for the
/// products that had enough (BR-INV-052).</item>
/// <item>Allocate in memory, first-expired-first, walking to the next batch when one is drained
/// (BR-INV-050).</item>
/// <item>Stage the batch decrements, the on-hand decreases (BR-INV-047), and one traceability
/// record per (sale line, batch) pair (BR-INV-057) — then return. The caller's single SaveChanges
/// commits everything atomically (BR-INV-048); a concurrent change to an <b>allocated</b> batch
/// fails there, never silently overwritten (BR-INV-056).</item>
/// </list>
///
/// Query count is constant — one for the candidates, one for the on-hand rows — regardless of how
/// many lines or batches are involved: no per-line query, no per-batch query, no N+1, no lazy
/// loading (BR-INV-053).
/// </summary>
public sealed class InventoryConsumptionWriter(
    VetFlowDbContext dbContext,
    IClinicClock clinicClock,
    TimeProvider timeProvider)
    : IInventoryConsumptionWriter
{
    public async Task<InventoryConsumptionResult> StageAsync(
        IReadOnlyCollection<InventoryConsumptionRequest> requests,
        CancellationToken cancellationToken)
    {
        EnsureRequestsAreConsumable(requests);

        // Distinct products in a stable order, so allocation — and any rejection message — is
        // reproducible for the same input (BR-INV-050: determinism is mandatory).
        var productIds = requests.Select(request => request.ProductId).Distinct().ToList();
        var today = clinicClock.Today;

        // One ordered candidate query for every product at once (BR-INV-053). The saleable
        // predicate is part of the WHERE clause: expired batches are never read and then filtered
        // in memory — they are excluded before allocation begins (DEC-INV-021). A batch with no
        // expiry date never expires and is saleable (BR-INV-051).
        var candidates = await dbContext.InventoryBatches
            .Where(batch => productIds.Contains(batch.ProductId)
                && batch.RemainingQuantity > 0m
                && (batch.ExpiryDate == null || batch.ExpiryDate >= today))
            // The total order of BR-INV-050: expiry ascending with NULLs last (stated explicitly,
            // never left to the database's default), then receive date, then the stable batch id.
            .OrderBy(batch => batch.ExpiryDate == null)
            .ThenBy(batch => batch.ExpiryDate)
            .ThenBy(batch => batch.ReceivedAt)
            .ThenBy(batch => batch.Id)
            .ToListAsync(cancellationToken);

        var saleableBatches = candidates
            .GroupBy(batch => batch.ProductId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var requestedByProduct = requests
            .GroupBy(request => request.ProductId)
            .ToDictionary(group => group.Key, group => group.Sum(request => request.StockQuantity));

        // Sufficiency is measured on the saleable batches, not on the projected on-hand balance:
        // a product whose balance reads positive but whose batches are all expired is short
        // (BR-INV-052, DEC-INV-021, AC-INV-045). Any shortfall rejects everything.
        var insufficientProductIds = productIds
            .Where(productId => SaleableQuantity(saleableBatches, productId) < requestedByProduct[productId])
            .ToList();
        if (insufficientProductIds.Count > 0)
        {
            return InventoryConsumptionResult.Insufficient(insufficientProductIds);
        }

        var onHandByProduct = await dbContext.ProductOnHands
            .Where(onHand => productIds.Contains(onHand.ProductId))
            .ToDictionaryAsync(onHand => onHand.ProductId, cancellationToken);

        var consumedAt = timeProvider.GetUtcNow();
        foreach (var productId in productIds)
        {
            var consumed = AllocateProduct(productId, requests, saleableBatches[productId], consumedAt);

            // The on-hand balance falls by exactly what the batches lost, in the same unit of work
            // — that is what keeps the BR-INV-005 invariant true under consumption (BR-INV-049).
            if (!onHandByProduct.TryGetValue(productId, out var onHand))
            {
                throw new InvalidOperationException(
                    $"Product {productId} has saleable batches but no on-hand record (BR-INV-005 broken).");
            }

            onHand.Decrease(consumed);
        }

        return InventoryConsumptionResult.Success;
    }

    /// <summary>
    /// FEFO allocation for one product (BR-INV-050). The product's requests are walked in their
    /// original order over a single shared batch cursor: the quantities are effectively aggregated
    /// for allocation — a product repeated on two lines is allocated once, against one state
    /// (BR-INV-046) — while <b>each</b> consumed amount is still recorded against the sale line
    /// that caused it (BR-INV-057, TS-INV-054). Returns the total consumed for the product.
    /// </summary>
    private decimal AllocateProduct(
        Guid productId,
        IEnumerable<InventoryConsumptionRequest> requests,
        List<InventoryBatch> batches,
        DateTimeOffset consumedAt)
    {
        var cursor = 0;
        var consumedForProduct = 0m;

        foreach (var request in requests.Where(request => request.ProductId == productId))
        {
            var outstanding = request.StockQuantity;

            while (outstanding > 0m)
            {
                if (cursor >= batches.Count)
                {
                    // Unreachable: sufficiency was checked above against these same batches.
                    throw new InvalidOperationException(
                        $"FEFO allocation ran out of saleable batches for product {productId}.");
                }

                var batch = batches[cursor];
                var take = Math.Min(batch.RemainingQuantity, outstanding);

                batch.Consume(take);
                dbContext.InventoryConsumptions.Add(new InventoryConsumption(
                    Guid.NewGuid(),
                    batch.Id,
                    productId,
                    request.SaleLineId,
                    take,
                    consumedAt));

                outstanding -= take;
                consumedForProduct += take;

                // A drained batch becomes "depleted" by the existing derivation (BR-INV-021); it
                // is kept, never deleted (BR-INV-047).
                if (batch.RemainingQuantity <= 0m)
                {
                    cursor++;
                }
            }
        }

        return consumedForProduct;
    }

    private static decimal SaleableQuantity(
        IReadOnlyDictionary<Guid, List<InventoryBatch>> saleableBatches,
        Guid productId) =>
        saleableBatches.TryGetValue(productId, out var batches)
            ? batches.Sum(batch => batch.RemainingQuantity)
            : 0m;

    /// <summary>
    /// A request must carry a positive quantity and the sale line it belongs to: sale-line-level
    /// traceability is a <b>precondition of acceptance</b>, not an afterthought, because it cannot
    /// be reconstructed later (BR-INV-046, BR-INV-057, REQ-INV-008).
    /// </summary>
    private static void EnsureRequestsAreConsumable(IReadOnlyCollection<InventoryConsumptionRequest> requests)
    {
        if (requests.Count == 0)
        {
            throw new BusinessRuleException(
                InventoryErrorCodes.ConsumptionRequestInvalid,
                new Dictionary<string, string> { ["reason"] = "noRequests" });
        }

        foreach (var request in requests)
        {
            if (request.ProductId == Guid.Empty)
            {
                throw new BusinessRuleException(
                    InventoryErrorCodes.ConsumptionRequestInvalid,
                    new Dictionary<string, string> { ["reason"] = "productMissing" });
            }

            if (request.SaleLineId == Guid.Empty)
            {
                throw new BusinessRuleException(
                    InventoryErrorCodes.ConsumptionRequestInvalid,
                    new Dictionary<string, string> { ["reason"] = "saleLineMissing" });
            }

            if (request.StockQuantity <= 0m)
            {
                throw new BusinessRuleException(
                    InventoryErrorCodes.ConsumptionRequestInvalid,
                    new Dictionary<string, string> { ["reason"] = "quantityNotPositive" });
            }
        }
    }
}
