using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Commands.CommitPurchaseReturn;
using VetFlow.Domain.Common;
using VetFlow.Domain.Inventory;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Inventory;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Commit-purchase-return write path (BR-PUR-018, AC-PUR-023/024/025) — the one place C5 moves
/// stock.
///
/// <para>The state transition belongs to the aggregate; the stock effect belongs to Inventory. This
/// handler joins them in <b>one</b> unit of work through <see cref="BatchOperationWriter"/>
/// (BR-INV-062, DEC-INV-019): every line decrements its own batch and the product on-hand together,
/// and one ledger row per line is written with type <c>PurchaseReturn</c>, source
/// <c>Purchasing</c>, and a reference to the return line — which is what makes the movement
/// traceable back to its document (BR-INV-057).</para>
///
/// <para><b>The over-return case needs no rule of its own here.</b> If the batch cannot absorb the
/// decrement, the aggregate's floor rule rejects it (BR-INV-061, VTF-INV-061) and nothing is saved
/// — the same guard that protects every other Epic 2 operation.</para>
///
/// <para><b>Batches are never re-selected at commit.</b> Each line already carries the batch its
/// original purchase line created (BR-PUR-017); FEFO plays no part in a return (BR-INV-069).</para>
///
/// <para>Returns <c>false</c> when the return does not exist (404). A committed return
/// (VTF-PUR-018), an empty one (VTF-PUR-019), a batch that would go below zero (VTF-INV-061) or a
/// concurrent batch change (VTF-INV-068) are all rejected with nothing applied.</para>
/// </summary>
public sealed class CommitPurchaseReturnCommandHandler(
    VetFlowDbContext dbContext,
    BatchOperationWriter batchWriter)
    : ICommandHandler<CommitPurchaseReturnCommand, bool>
{
    public async Task<bool> HandleAsync(CommitPurchaseReturnCommand command, CancellationToken cancellationToken)
    {
        var purchaseReturn = await dbContext.PurchaseReturns
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == command.PurchaseReturnId, cancellationToken);

        if (purchaseReturn is null)
        {
            return false;
        }

        // Transition first: it enforces Draft-only and non-empty (BR-PUR-018) and throws before any
        // stock is touched, so a rejected commit cannot leave a partial movement behind.
        purchaseReturn.Commit();

        // Return-line quantities are expressed in the ORIGINAL LINE'S PURCHASE UNIT — that is what
        // BR-PUR-016 caps and what the screen shows — while batches hold the product's smallest
        // stock unit (BR-INV-058). Returning 4 cartons of a 10-carton line must therefore remove
        // 480 stock units, not 4.
        //
        // The factor is derived from the RECEIPT ITSELF (the batch's original received quantity
        // over the original line's quantity), not from today's catalog conversion. If a product's
        // conversion factor were edited after receiving, the catalog would give a factor that never
        // applied to this stock and the return would move the wrong amount; the receipt cannot
        // disagree with itself. `InventoryBatch.Quantity` is the historical received amount and
        // never changes, so the ratio is stable for the life of the batch.
        var lineIds = purchaseReturn.Lines.Select(line => line.PurchaseLineItemId).Distinct().ToList();
        var batchIds = purchaseReturn.Lines.Select(line => line.BatchId).Distinct().ToList();

        var originalQuantities = await dbContext.PurchaseLineItems
            .Where(line => lineIds.Contains(line.Id))
            .ToDictionaryAsync(line => line.Id, line => line.Quantity, cancellationToken);

        var receivedQuantities = await dbContext.InventoryBatches
            .Where(batch => batchIds.Contains(batch.Id))
            .ToDictionaryAsync(batch => batch.Id, batch => batch.Quantity, cancellationToken);

        var deltas = new List<BatchOperationWriter.DocumentBatchDelta>(purchaseReturn.Lines.Count);
        foreach (var line in purchaseReturn.Lines)
        {
            if (!originalQuantities.TryGetValue(line.PurchaseLineItemId, out var originalQuantity)
                || !receivedQuantities.TryGetValue(line.BatchId, out var receivedQuantity)
                || originalQuantity <= 0m)
            {
                // The originating line or batch vanished between add and commit. Committing the
                // document without its stock effect would break BR-INV-062, so it fails loudly.
                throw new BusinessRuleException(PurchasingErrorCodes.ReturnLineComposition);
            }

            // Multiply before dividing so the factor is never materialized on its own — a factor
            // like 1000/3 would lose fidelity, and quantities are never rounded (BR-INV-058).
            var stockQuantity = line.Quantity * receivedQuantity / originalQuantity;

            // A purchase return takes stock OUT — the goods go back to the supplier — so every
            // delta is negative (BR-INV-064: the sign convention lives in one place).
            deltas.Add(new BatchOperationWriter.DocumentBatchDelta(line.BatchId, -stockQuantity, line.Id));
        }

        var movementIds = await batchWriter.ApplyDocumentAsync(
            deltas,
            InventoryMovementType.PurchaseReturn,
            InventoryMovementSource.Purchasing,
            cancellationToken);

        if (movementIds is null)
        {
            // A line's batch vanished between add and commit. That is not a business outcome the
            // user can act on, and silently committing the document without its stock effect would
            // break BR-INV-062 — so it fails loudly with nothing saved.
            throw new BusinessRuleException(PurchasingErrorCodes.ReturnLineComposition);
        }

        return true;
    }
}
