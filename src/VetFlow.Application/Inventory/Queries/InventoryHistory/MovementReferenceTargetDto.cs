namespace VetFlow.Application.Inventory.Queries.InventoryHistory;

/// <summary>
/// What a history row's reference opens (BR-INV-043). Only the targets that exist today are
/// enumerated — the same discipline BR-INV-065 applies to movement types: no value without a real
/// destination. Purchase and sales <i>return</i> documents join this set when C5/C6 build them.
/// </summary>
public enum MovementReferenceTargetDto
{
    /// <summary>Inventory-native operation — no counterparty document, rendered as "—" (DEC-INV-036).</summary>
    None = 0,

    /// <summary>Purchase invoice details, /purchases/:id (REQ-PUR-002, DEC-INV-010).</summary>
    PurchaseInvoice = 1,

    /// <summary>Sales invoice details, /sales/:id (REQ-SAL-002).</summary>
    SalesInvoice = 2,
}
