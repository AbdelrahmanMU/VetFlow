namespace VetFlow.Application.Inventory.Queries.InventoryHistory;

/// <summary>
/// The movement type as the API contract expresses it — a 1:1 mirror of
/// <see cref="Domain.Inventory.InventoryMovementType"/> kept separate so the wire contract is not
/// pinned to a domain enum (the <c>SalesInvoiceStatusDto</c> precedent). The set is closed by
/// BR-INV-065: every value has a writing path, and none is enumerated speculatively.
/// </summary>
public enum InventoryMovementTypeDto
{
    Receive = 1,
    Consume = 2,
    Adjustment = 3,
    WriteOff = 4,
    PurchaseReturn = 5,
    SalesReturn = 6,
}
