namespace VetFlow.Application.Inventory.Queries.InventoryProjection;

/// <summary>Whitelisted sort fields of the inventory projection (BR-INV-014, STD-API-023).</summary>
public enum InventoryProjectionSortField
{
    Product = 1,
    OnHand = 2,
    NearestExpiry = 3,
}
