namespace VetFlow.Application.Inventory.Commands.WriteOffInventory;

/// <summary>
/// The reason codes a <b>write-off</b> accepts — the owner's list, verbatim (DEC-INV-031,
/// BR-INV-067): منتهي الصلاحية · تالف · مفقود · ملوَّث · أخرى.
///
/// <para>Deliberately <b>not</b> the adjustment list: <c>CountCorrection</c>, <c>InitialBalance</c>
/// and <c>Found</c> are absent because the owner ruled the two vocabularies separately — and
/// «موجود» on a write-off would be a contradiction in terms. The values mirror
/// <c>InventoryMovementReason</c> so the two never drift, which an architecture test asserts.</para>
/// </summary>
public enum WriteOffReason
{
    Damaged = 3,
    Lost = 5,
    Expired = 6,
    Contaminated = 7,
    Other = 8,
}
