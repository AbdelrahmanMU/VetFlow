namespace VetFlow.Domain.Inventory;

/// <summary>
/// The module that caused a movement (BR-INV-064), carried so the history screen can show it
/// without resolving the reference first (the preserved BR-INV-043 shape).
/// </summary>
public enum InventoryMovementSource
{
    Purchasing = 1,
    Sales = 2,

    /// <summary>Inventory-native operations that have no counterparty document (DEC-INV-036).</summary>
    Inventory = 3,
}
