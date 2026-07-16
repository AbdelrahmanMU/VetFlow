namespace VetFlow.Application.Purchasing.Queries.PurchaseList;

/// <summary>Whitelisted sort fields of the purchase list (BR-PUR-004, STD-API-023).</summary>
public enum PurchaseListSortField
{
    Number = 1,
    InvoiceDate = 2,
    Supplier = 3,
    Status = 4,
    Total = 5,
}
