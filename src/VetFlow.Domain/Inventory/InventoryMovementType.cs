namespace VetFlow.Domain.Inventory;

/// <summary>
/// The closed set of inventory movement types (BR-INV-065). Every value has a writing path that
/// exists in the system — no type is enumerated speculatively, continuing the rule BR-INV-042 set
/// for the deferred history design: never invent business vocabulary that has no source.
/// Transfers and reservations are deliberately absent (out of Epic 2 scope, DEC-INV-035).
/// </summary>
public enum InventoryMovementType
{
    /// <summary>Purchase receiving — the movement that creates a batch (BR-PUR-010).</summary>
    Receive = 1,

    /// <summary>A committed sale consuming stock through FEFO allocation (REQ-INV-006).</summary>
    Consume = 2,

    /// <summary>A batch-level correction in either direction (REQ-INV-010, DEC-INV-032).</summary>
    Adjustment = 3,

    /// <summary>Unusable stock leaving inventory (REQ-INV-011).</summary>
    WriteOff = 4,

    /// <summary>Stock returned to the supplier, bound to its originating batch (BR-INV-069).</summary>
    PurchaseReturn = 5,

    /// <summary>Stock returned by a customer, restored to its originating batch (BR-INV-069).</summary>
    SalesReturn = 6,
}
