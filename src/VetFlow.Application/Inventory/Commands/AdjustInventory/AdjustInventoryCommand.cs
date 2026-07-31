using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Commands.AdjustInventory;

/// <summary>
/// Correct a single batch's quantity in either direction (REQ-INV-010, DEC-INV-032) — the first
/// inventory operation a person performs directly, with no counterparty document (DEC-INV-036) and
/// therefore no reference on the ledger row it writes.
///
/// <para>The caller states a <b>direction</b> and a <b>positive magnitude</b> rather than a signed
/// number: the sign convention belongs to the domain (BR-INV-064), not to whoever fills the form,
/// and a mistyped minus must not silently become the opposite operation.</para>
///
/// <para>Returns the movement id, or <c>null</c> when the batch does not exist (⇒ 404).</para>
/// </summary>
public sealed record AdjustInventoryCommand : ICommand<Guid?>
{
    public const int MaxReasonNoteLength = 500;
    public const int MaxActorNameLength = 100;

    public required Guid BatchId { get; init; }

    public required AdjustmentDirection Direction { get; init; }

    /// <summary>The magnitude in the product's stock unit — always positive, never rounded (BR-INV-058).</summary>
    public required decimal Quantity { get; init; }

    /// <summary>Mandatory, and only from the adjustment list (BR-INV-067, DEC-INV-031).</summary>
    public required AdjustmentReason Reason { get; init; }

    /// <summary>Optional free-text note accompanying the reason (BR-INV-067).</summary>
    public string? ReasonNote { get; init; }

    /// <summary>
    /// Optional free-text actor (BR-INV-066, DEC-INV-030). There is no users module and no
    /// authentication by explicit owner ruling: this is never validated and its absence never
    /// blocks the operation.
    /// </summary>
    public string? ActorName { get; init; }
}
