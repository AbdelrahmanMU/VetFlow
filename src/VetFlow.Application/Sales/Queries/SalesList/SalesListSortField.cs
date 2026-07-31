namespace VetFlow.Application.Sales.Queries.SalesList;

/// <summary>Whitelisted sort fields of the sales list (BR-SAL-019, STD-API-023).</summary>
public enum SalesListSortField
{
    Number = 1,
    SaleDate = 2,
    Customer = 3,
    Status = 4,
    Total = 5,
}
