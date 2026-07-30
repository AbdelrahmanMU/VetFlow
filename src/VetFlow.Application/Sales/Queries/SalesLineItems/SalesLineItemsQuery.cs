using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Queries.SalesLineItems;

/// <summary>
/// Read the lines of one sales invoice (REQ-SAL-002). <c>null</c> ⇒ the invoice does not exist
/// (404); an existing invoice with no lines returns an empty list (BR-SAL-004).
/// </summary>
public sealed record SalesLineItemsQuery : IQuery<IReadOnlyList<SalesLineItemDto>?>
{
    public required Guid InvoiceId { get; init; }
}
