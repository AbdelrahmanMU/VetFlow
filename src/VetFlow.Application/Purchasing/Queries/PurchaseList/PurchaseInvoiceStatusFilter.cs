namespace VetFlow.Application.Purchasing.Queries.PurchaseList;

/// <summary>The status list filter (BR-PUR-004); absent means all three states.</summary>
public enum PurchaseInvoiceStatusFilter
{
    Draft = 1,
    Received = 2,
    Cancelled = 3,
}
