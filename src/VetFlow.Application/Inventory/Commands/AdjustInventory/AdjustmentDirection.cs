namespace VetFlow.Application.Inventory.Commands.AdjustInventory;

/// <summary>
/// Which way an adjustment moves the batch (DEC-INV-032). Explicit rather than a signed quantity
/// so the caller states an intention and the domain owns the sign (BR-INV-064).
/// </summary>
public enum AdjustmentDirection
{
    Increase = 1,
    Decrease = 2,
}
