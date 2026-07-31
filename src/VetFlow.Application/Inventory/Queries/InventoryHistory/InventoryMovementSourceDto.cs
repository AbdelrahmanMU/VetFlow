namespace VetFlow.Application.Inventory.Queries.InventoryHistory;

/// <summary>
/// The causing module as the API contract expresses it — a 1:1 mirror of
/// <see cref="Domain.Inventory.InventoryMovementSource"/> (BR-INV-043, DEC-INV-016).
/// </summary>
public enum InventoryMovementSourceDto
{
    Purchasing = 1,
    Sales = 2,
    Inventory = 3,
}
