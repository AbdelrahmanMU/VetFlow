namespace VetFlow.Application.Inventory.Queries.BatchViewer;

/// <summary>Whitelisted sort fields of the batch viewer (BR-INV-027, STD-API-023).</summary>
public enum BatchViewerSortField
{
    ReceiveDate = 1,
    ExpiryDate = 2,
    RemainingQuantity = 3,
}
