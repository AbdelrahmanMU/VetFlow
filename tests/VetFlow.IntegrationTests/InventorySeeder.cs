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
}
