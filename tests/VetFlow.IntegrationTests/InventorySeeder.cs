using VetFlow.Domain.Inventory;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Seeds inventory read-model state — <see cref="ProductOnHand"/> and
/// <see cref="InventoryBatch"/> — directly through the write-kernel domain
/// constructors, the way receiving produces it. The projection is a read-only
/// view over this state (write-kernel.md); its tests set the state up rather
/// than drive full receiving (a read-model test needs the rows, not the flow).
/// </summary>
public static class InventorySeeder
{
    /// <summary>Creates the product's on-hand record with the given quantity (0 = none received).</summary>
    public static void SetOnHand(VetFlowDbContext dbContext, Guid productId, decimal quantity)
    {
        var onHand = new ProductOnHand(productId);
        if (quantity > 0m)
        {
            onHand.Increase(quantity);
        }

        dbContext.ProductOnHands.Add(onHand);
    }

    /// <summary>Adds one active inventory batch for the product (RemainingQuantity = quantity).</summary>
    public static InventoryBatch AddBatch(
        VetFlowDbContext dbContext,
        Guid productId,
        decimal quantity,
        DateOnly? expiryDate = null,
        decimal unitCost = 100m)
    {
        var batch = new InventoryBatch(
            Guid.NewGuid(),
            productId,
            // Synthetic provenance id — reference-only, no cross-module FK (BR-INV-001).
            Guid.NewGuid(),
            quantity,
            unitCost,
            expiryDate,
            DateTimeOffset.UtcNow);

        dbContext.InventoryBatches.Add(batch);
        return batch;
    }

    /// <summary>
    /// Adds an inventory batch with real purchase provenance — a draft invoice + line the
    /// batch's <see cref="InventoryBatch.PurchaseLineId"/> points at, the way receiving
    /// produces it. The Batch Viewer resolves the purchase reference through this chain
    /// (BR-INV-024), so its tests need a resolvable line, not a synthetic id.
    /// </summary>
    public static async Task<(InventoryBatch Batch, Domain.Purchasing.PurchaseInvoice Invoice)> AddBatchWithProvenanceAsync(
        VetFlowDbContext dbContext,
        Guid productId,
        string productName,
        decimal quantity,
        DateOnly? expiryDate = null,
        decimal unitCost = 100m,
        DateTimeOffset? receivedAt = null)
    {
        // Each call stamps its own "now" unless the caller pins one — a test about the
        // receive-date tie-break needs the dates to actually tie (BR-INV-031).
        var stamp = receivedAt ?? DateTimeOffset.UtcNow;
        var invoice = await PurchasingSeeder.NewInvoiceAsync(
            dbContext, $"مورد-{Guid.NewGuid():N}", DateOnly.FromDateTime(DateTime.UtcNow), quantity * unitCost);
        var line = invoice.AddLine(
            Guid.NewGuid(), productId, productName, Guid.NewGuid(), "علبة", quantity, unitCost, stamp);

        var batch = new InventoryBatch(
            Guid.NewGuid(), productId, line.Id, quantity, unitCost, expiryDate, stamp);
        dbContext.InventoryBatches.Add(batch);
        return (batch, invoice);
    }

    /// <summary>
    /// Forces a batch to the depleted state (RemainingQuantity = 0) for forward-compatible
    /// tests — no consumption path exists this slice, so the private setter is driven directly
    /// (the PurchasingSeeder.SetStatus precedent).
    /// </summary>
    public static void MarkDepleted(VetFlowDbContext dbContext, InventoryBatch batch) =>
        dbContext.Entry(batch).Property(entity => entity.RemainingQuantity).CurrentValue = 0m;
}
