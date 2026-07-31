using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Commands.WriteOffInventory;
using VetFlow.Domain.Inventory;

namespace VetFlow.Infrastructure.Inventory;

/// <summary>
/// Write-off (REQ-INV-011) — **closes R9**: expired stock was visible, unsaleable (DEC-INV-021) and
/// stuck inside <c>OnHandQuantity</c> with no way out. This is that way out.
///
/// <para>Two things make it different from an adjustment, and they are the only two: the quantity
/// is <b>always removed</b> — there is no increasing write-off — and the reason must come from the
/// write-off list. Everything else is the shared <see cref="BatchOperationWriter"/>.</para>
///
/// <para><b>An expired batch is deliberately not excluded.</b> DEC-INV-021 keeps expired stock out
/// of <i>sales</i> allocation; refusing to write it off would be the opposite of what R9 asked
/// for.</para>
/// </summary>
public sealed class WriteOffInventoryCommandHandler(BatchOperationWriter writer)
    : ICommandHandler<WriteOffInventoryCommand, Guid?>
{
    public Task<Guid?> HandleAsync(WriteOffInventoryCommand command, CancellationToken cancellationToken) =>
        writer.ApplyAsync(
            command.BatchId,
            -command.Quantity,
            InventoryMovementType.WriteOff,
            (InventoryMovementReason)command.Reason,
            InventoryMovementReasons.ForWriteOff,
            command.ReasonNote,
            command.ActorName,
            cancellationToken);
}
