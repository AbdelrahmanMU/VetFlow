using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Queries.PurchaseLineItems;

/// <summary>
/// List the line items of one purchase invoice (REQ-PUR-004), ordered by add time.
/// A <c>null</c> result means the invoice does not exist and the endpoint answers 404;
/// an existing invoice with no lines returns an empty list (BR-PUR-005 — an invoice may
/// temporarily hold zero lines).
/// </summary>
public sealed record PurchaseLineItemsQuery : IQuery<IReadOnlyList<PurchaseLineItemDto>?>
{
    public required Guid InvoiceId { get; init; }
}
