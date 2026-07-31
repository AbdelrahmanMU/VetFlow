using Microsoft.EntityFrameworkCore;
using VetFlow.Domain.Common;
using VetFlow.Domain.Inventory;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Inventory;

/// <summary>
/// The one write path shared by every Epic 2 operation that moves an <b>existing</b> batch —
/// adjustments (REQ-INV-010) and write-off (REQ-INV-011) today, the two returns next. It exists so
/// the rules they have in common are implemented once: the three things that must move together,
/// the reason-list check, and the concurrency outcome.
///
/// <para>Writing it per capability would mean four copies of BR-INV-005, BR-INV-062, BR-INV-067 and
/// BR-INV-068 drifting apart one paste at a time. What differs between operations — the movement
/// type, the direction, and which reason list applies — is passed in; nothing else does.</para>
///
/// <para>It stages and saves as <b>one</b> unit of work (BR-INV-003): the batch, the on-hand
/// quantity and the ledger row commit together or not at all.</para>
/// </summary>
public sealed class BatchOperationWriter(VetFlowDbContext dbContext, TimeProvider timeProvider)
{
    /// <summary>
    /// One batch movement requested by a document line (<see cref="ApplyDocumentAsync"/>):
    /// which batch, the signed change, and the document line that caused it.
    /// </summary>
    public readonly record struct DocumentBatchDelta(Guid BatchId, decimal Delta, Guid ReferenceId);

    /// <summary>
    /// Applies the movements of a whole <b>document</b> — a purchase or sales return
    /// (REQ-PUR-006 / REQ-SAL-004) — as a <b>single</b> unit of work.
    ///
    /// <para>Two things make this different from <see cref="ApplyAsync"/>, and both come from the
    /// rules rather than from convenience:</para>
    /// <list type="number">
    ///   <item><description><b>All lines commit together or none does</b> (BR-PUR-018 /
    ///   BR-SAL-018). <see cref="ApplyAsync"/> saves once per call, which is right for an
    ///   inventory-native operation — one batch, one movement — but would make a three-line return
    ///   three separate transactions, and a failure on the third would leave the first two
    ///   applied. Here everything is staged and saved once.</description></item>
    ///   <item><description><b>No reason, and a document source.</b> Returns carry no reason code
    ///   at all (BR-INV-067 — «مستندها هو سياقها»), so there is no reason parameter to pass and no
    ///   reason list to check. Instead each movement carries the <see cref="InventoryMovementSource"/>
    ///   of the module that owns the document and a <c>ReferenceId</c> pointing at the return line,
    ///   which is what makes the movement traceable back to its document (BR-INV-057).</description></item>
    /// </list>
    ///
    /// <para>Returns the movement ids, or <c>null</c> when any batch does not exist (⇒ 404) —
    /// checked before anything is mutated. Throws <see cref="BusinessRuleException"/> for a change
    /// that would go below zero (VTF-INV-061, raised by the aggregate — this is also the
    /// over-return rejection, needing no rule of its own) or a concurrent change (VTF-INV-068).</para>
    /// </summary>
    public async Task<IReadOnlyList<Guid>?> ApplyDocumentAsync(
        IReadOnlyList<DocumentBatchDelta> deltas,
        InventoryMovementType type,
        InventoryMovementSource source,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfZero(deltas.Count);

        var batchIds = deltas.Select(delta => delta.BatchId).Distinct().ToList();
        var batches = await dbContext.InventoryBatches
            .Where(batch => batchIds.Contains(batch.Id))
            .ToDictionaryAsync(batch => batch.Id, cancellationToken);

        if (batches.Count != batchIds.Count)
        {
            return null;
        }

        var productIds = batches.Values.Select(batch => batch.ProductId).Distinct().ToList();
        var onHands = await dbContext.ProductOnHands
            .Where(item => productIds.Contains(item.ProductId))
            .ToDictionaryAsync(item => item.ProductId, cancellationToken);

        var movementIds = new List<Guid>(deltas.Count);
        var occurredAt = timeProvider.GetUtcNow();

        foreach (var (batchId, delta, referenceId) in deltas)
        {
            var batch = batches[batchId];
            if (!onHands.TryGetValue(batch.ProductId, out var onHand))
            {
                // Same invariant as ApplyAsync: a batch always has an on-hand row (BR-INV-005).
                // Its absence is corruption, not a business outcome, and creating one here would
                // hide the very drift the invariant exists to prevent.
                throw new InvalidOperationException(
                    $"Product {batch.ProductId} has an inventory batch but no on-hand row (BR-INV-005).");
            }

            // The floor rule lives on the aggregate and rejects rather than clamps (BR-INV-061,
            // DEC-INV-032). Nothing further is staged if it throws, and nothing has been saved yet.
            batch.ApplyDelta(delta);
            onHand.ApplyDelta(delta);

            var movement = delta > 0m
                ? InventoryMovement.Increase(
                    Guid.NewGuid(), batch.ProductId, batch.Id, type, source,
                    delta, occurredAt, referenceId: referenceId)
                : InventoryMovement.Decrease(
                    Guid.NewGuid(), batch.ProductId, batch.Id, type, source,
                    -delta, occurredAt, referenceId: referenceId);

            dbContext.InventoryMovements.Add(movement);
            movementIds.Add(movement.Id);
        }

        try
        {
            // One SaveChanges for every line of the document (BR-INV-062).
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BusinessRuleException(
                InventoryErrorCodes.OperationConcurrencyConflict,
                new Dictionary<string, string> { ["batchId"] = string.Join(",", batchIds) });
        }

        return movementIds;
    }

    /// <summary>
    /// Applies a signed change to one batch. Returns the movement id, or <c>null</c> when the batch
    /// does not exist (⇒ 404). Throws <see cref="BusinessRuleException"/> for a reason outside the
    /// operation's list (VTF-INV-067), a change that would go below zero (VTF-INV-061, raised by the
    /// aggregate), or a concurrent change to the batch (VTF-INV-068).
    /// </summary>
    public async Task<Guid?> ApplyAsync(
        Guid batchId,
        decimal delta,
        InventoryMovementType type,
        InventoryMovementReason reason,
        IReadOnlySet<InventoryMovementReason> allowedReasons,
        string? reasonNote,
        string? actorName,
        CancellationToken cancellationToken)
    {
        // Checked before any read, so an illegal reason costs nothing. The two lists are the owner's
        // and are deliberately different (BR-INV-067, DEC-INV-031).
        if (!allowedReasons.Contains(reason))
        {
            throw new BusinessRuleException(
                InventoryErrorCodes.ReasonNotAllowed,
                new Dictionary<string, string> { ["reason"] = reason.ToString(), ["operation"] = type.ToString() });
        }

        var batch = await dbContext.InventoryBatches
            .FirstOrDefaultAsync(candidate => candidate.Id == batchId, cancellationToken);
        if (batch is null)
        {
            return null;
        }

        var onHand = await dbContext.ProductOnHands
            .FirstOrDefaultAsync(item => item.ProductId == batch.ProductId, cancellationToken);
        if (onHand is null)
        {
            // A batch always has an on-hand row: receiving creates both in one transaction
            // (BR-INV-005). Its absence is corruption, not a business outcome, and must not be
            // papered over by creating one here — that would hide the very drift the invariant
            // exists to prevent.
            throw new InvalidOperationException(
                $"Product {batch.ProductId} has an inventory batch but no on-hand row (BR-INV-005).");
        }

        // The floor rule lives on the aggregate and rejects rather than clamps (BR-INV-061,
        // DEC-INV-032). Nothing below runs if it throws.
        batch.ApplyDelta(delta);
        onHand.ApplyDelta(delta);

        var movement = delta > 0m
            ? InventoryMovement.Increase(
                Guid.NewGuid(), batch.ProductId, batch.Id, type, InventoryMovementSource.Inventory,
                delta, timeProvider.GetUtcNow(), reason: reason, reasonNote: reasonNote, actorName: actorName)
            : InventoryMovement.Decrease(
                Guid.NewGuid(), batch.ProductId, batch.Id, type, InventoryMovementSource.Inventory,
                -delta, timeProvider.GetUtcNow(), reason: reason, reasonNote: reasonNote, actorName: actorName);

        // No reference id: these are inventory-native operations with no counterparty document
        // (DEC-INV-036), which is why the history renders "—" for them (BR-INV-043).
        dbContext.InventoryMovements.Add(movement);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The batch carries a row-version token (the xmin mechanism of DEC-INV-023), so a batch
            // that changed under us fails the UPDATE instead of overwriting silently. BR-INV-068
            // requires this for decreasing operations; an increase inherits the same detection
            // because it goes through the same row — stricter than ruled, and deliberately kept.
            throw new BusinessRuleException(
                InventoryErrorCodes.OperationConcurrencyConflict,
                new Dictionary<string, string> { ["batchId"] = batch.Id.ToString() });
        }

        return movement.Id;
    }
}
