namespace VetFlow.Domain.Inventory;

/// <summary>
/// The fixed reason codes the owner ruled on 2026-07-31 (DEC-INV-031, BR-INV-067). The two lists
/// overlap deliberately — <c>Damaged</c>, <c>Lost</c> and <c>Other</c> belong to both — so they
/// live in one enum, and each operation validates the subset it accepts rather than duplicating
/// the vocabulary. Nothing here was invented: the terms are the owner's, verbatim.
///
/// Adjustments: CountCorrection · InitialBalance · Damaged · Found · Lost · Other.
/// Write-off:   Expired · Damaged · Lost · Contaminated · Other.
/// </summary>
public enum InventoryMovementReason
{
    CountCorrection = 1,
    InitialBalance = 2,
    Damaged = 3,
    Found = 4,
    Lost = 5,
    Expired = 6,
    Contaminated = 7,
    Other = 8,
}

/// <summary>The reason subsets each operation accepts (BR-INV-067).</summary>
public static class InventoryMovementReasons
{
    public static readonly IReadOnlySet<InventoryMovementReason> ForAdjustment = new HashSet<InventoryMovementReason>
    {
        InventoryMovementReason.CountCorrection,
        InventoryMovementReason.InitialBalance,
        InventoryMovementReason.Damaged,
        InventoryMovementReason.Found,
        InventoryMovementReason.Lost,
        InventoryMovementReason.Other,
    };

    public static readonly IReadOnlySet<InventoryMovementReason> ForWriteOff = new HashSet<InventoryMovementReason>
    {
        InventoryMovementReason.Expired,
        InventoryMovementReason.Damaged,
        InventoryMovementReason.Lost,
        InventoryMovementReason.Contaminated,
        InventoryMovementReason.Other,
    };
}
