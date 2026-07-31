using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Commands.AdjustInventory;
using VetFlow.Domain.Inventory;

namespace VetFlow.Infrastructure.Inventory;

/// <summary>
/// Inventory adjustment (REQ-INV-010, DEC-INV-032) — the first stock change the clinic performs
/// directly, with no counterparty document (DEC-INV-036).
///
/// <para>What is specific to an adjustment is here — <b>both directions</b> are allowed, and the
/// reason must come from the adjustment list. Everything it shares with write-off and the returns
/// lives in <see cref="BatchOperationWriter"/>: the batch, the on-hand quantity and the ledger row
/// moving together in one unit of work (BR-INV-003/005/062), the floor rule (BR-INV-061), and the
/// concurrency outcome (BR-INV-068).</para>
///
/// <para>Keeping the on-hand quantity in step with the batch is what closes <b>R5</b>: the
/// BR-INV-005 invariant now has a correction mechanism instead of only a definition.</para>
/// </summary>
public sealed class AdjustInventoryCommandHandler(BatchOperationWriter writer)
    : ICommandHandler<AdjustInventoryCommand, Guid?>
{
    public Task<Guid?> HandleAsync(AdjustInventoryCommand command, CancellationToken cancellationToken)
    {
        // The magnitude is always positive on the wire; the direction decides the sign, once
        // (BR-INV-064), so a stray minus can never silently invert the operation.
        var delta = command.Direction == AdjustmentDirection.Increase ? command.Quantity : -command.Quantity;

        return writer.ApplyAsync(
            command.BatchId,
            delta,
            InventoryMovementType.Adjustment,
            (InventoryMovementReason)command.Reason,
            InventoryMovementReasons.ForAdjustment,
            command.ReasonNote,
            command.ActorName,
            cancellationToken);
    }
}
