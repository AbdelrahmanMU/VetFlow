namespace VetFlow.Application.Inventory.Queries.BatchViewer;

/// <summary>
/// Derived status of an inventory batch (BR-INV-021, DEC-INV-011). Computed from
/// <see cref="Domain.Inventory.InventoryBatch.RemainingQuantity"/> at query time and never
/// stored — only two values exist. "Expired" is never a status, only a filter (DEC-INV-012).
/// </summary>
public enum BatchStatus
{
    /// <summary>RemainingQuantity &gt; 0.</summary>
    Active = 1,

    /// <summary>RemainingQuantity == 0.</summary>
    Depleted = 2,
}
