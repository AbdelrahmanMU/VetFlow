using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Commands.WriteOffInventory;

/// <summary>
/// Take unusable stock out of a batch (REQ-INV-011) — the capability R9 named as the way out for
/// expired stock that is visible, unsaleable, and until now had no exit (DEC-INV-021).
///
/// <para><b>There is no direction.</b> A write-off only ever removes: unlike an adjustment it
/// cannot add, so the command carries a positive magnitude and nothing else. Offering a direction
/// here would invent a business capability nobody ruled.</para>
///
/// <para>Returns the movement id, or <c>null</c> when the batch does not exist (⇒ 404).</para>
/// </summary>
public sealed record WriteOffInventoryCommand : ICommand<Guid?>
{
    public const int MaxReasonNoteLength = 500;
    public const int MaxActorNameLength = 100;

    public required Guid BatchId { get; init; }

    /// <summary>The quantity to remove, in the product's stock unit — positive, never rounded (BR-INV-058).</summary>
    public required decimal Quantity { get; init; }

    /// <summary>Mandatory, and only from the write-off list (BR-INV-067, DEC-INV-031).</summary>
    public required WriteOffReason Reason { get; init; }

    public string? ReasonNote { get; init; }

    /// <summary>Optional free-text actor; its absence never blocks the operation (BR-INV-066).</summary>
    public string? ActorName { get; init; }
}
