using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Inventory;
using VetFlow.Domain.Inventory;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Inventory;

/// <summary>
/// The Inventory write kernel (write-kernel.md, DEC-INV-001) — implements the public write contract
/// Purchase Receiving depends on. For each received line it stages one <see cref="InventoryBatch"/>
/// (BR-INV-001) and increases the product's persisted on-hand quantity (BR-INV-002) on the shared
/// unit of work, <b>without committing</b>: the caller's single SaveChanges makes the receipt atomic
/// (BR-INV-003). No queries, projection, adjustments, returns, or history (BR-INV-004).
/// </summary>
public sealed class InventoryReceiptWriter(VetFlowDbContext dbContext) : IInventoryReceiptWriter
{
    public async Task StageAsync(IReadOnlyCollection<InventoryReceiptLine> lines, CancellationToken cancellationToken)
    {
        foreach (var line in lines)
        {
            var batch = new InventoryBatch(
                Guid.NewGuid(),
                line.ProductId,
                line.PurchaseLineId,
                line.StockQuantity,
                line.UnitCost,
                line.ExpiryDate,
                line.ReceivedAt);
            dbContext.InventoryBatches.Add(batch);

            // Every stock change writes exactly one ledger row, in the same unit of work as the
            // change itself (REQ-INV-009, BR-INV-062). Receiving is the movement that creates the
            // batch, so the reference is the purchase line and the source is Purchasing
            // (BR-INV-064/065, the preserved BR-INV-043 shape).
            dbContext.InventoryMovements.Add(InventoryMovement.Increase(
                Guid.NewGuid(),
                line.ProductId,
                batch.Id,
                InventoryMovementType.Receive,
                InventoryMovementSource.Purchasing,
                line.StockQuantity,
                line.ReceivedAt,
                referenceId: line.PurchaseLineId));

            // One on-hand row per product: reuse a row already tracked in this unit of work (the same
            // product appearing on two lines) before hitting the store, so it is incremented once per
            // line, never duplicated.
            var onHand = dbContext.ProductOnHands.Local.FirstOrDefault(item => item.ProductId == line.ProductId)
                ?? await dbContext.ProductOnHands.FirstOrDefaultAsync(
                    item => item.ProductId == line.ProductId, cancellationToken);
            if (onHand is null)
            {
                onHand = new ProductOnHand(line.ProductId);
                dbContext.ProductOnHands.Add(onHand);
            }

            onHand.Increase(line.StockQuantity);
        }
    }
}
