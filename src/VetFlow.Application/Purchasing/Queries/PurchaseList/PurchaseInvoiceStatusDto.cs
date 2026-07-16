namespace VetFlow.Application.Purchasing.Queries.PurchaseList;

/// <summary>Purchase-invoice status as exposed by the API contract (BR-PUR-003).</summary>
public enum PurchaseInvoiceStatusDto
{
    Draft = 1,
    Received = 2,
    Cancelled = 3,
}
