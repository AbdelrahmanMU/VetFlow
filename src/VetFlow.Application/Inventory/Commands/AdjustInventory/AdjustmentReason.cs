namespace VetFlow.Application.Inventory.Commands.AdjustInventory;

/// <summary>
/// The reason codes an <b>adjustment</b> accepts — the owner's list, verbatim (DEC-INV-031,
/// BR-INV-067): تصحيح جرد · رصيد افتتاحيّ · تالف · موجود · مفقود · أخرى.
///
/// <para>Deliberately <b>not</b> the whole domain vocabulary: <c>Expired</c> and
/// <c>Contaminated</c> belong to write-off alone, and the owner ruled the two lists separately. A
/// caller cannot even express them here, so the API contract enforces the split before validation
/// does — the domain-side subset <c>InventoryMovementReasons.ForAdjustment</c> is the second
/// guard, not the only one.</para>
/// </summary>
public enum AdjustmentReason
{
    CountCorrection = 1,
    InitialBalance = 2,
    Damaged = 3,
    Found = 4,
    Lost = 5,
    Other = 8,
}
