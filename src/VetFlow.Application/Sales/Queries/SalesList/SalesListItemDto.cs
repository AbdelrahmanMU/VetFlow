using VetFlow.Application.Common;
using VetFlow.Application.Sales.Queries.SalesDetails;

namespace VetFlow.Application.Sales.Queries.SalesList;

/// <summary>
/// One row of the sales list (REQ-SAL-005): the six frozen columns — system
/// number, customer name (optional — DEC-SAL-002), sale date, status, total,
/// and creation timestamp (BR-SAL-019). No stock, no line items. The status
/// DTO is reused from the details query rather than duplicated.
/// </summary>
public sealed record SalesListItemDto
{
    public required Guid Id { get; init; }

    public required string Number { get; init; }

    public string? CustomerName { get; init; }

    public required DateOnly SaleDate { get; init; }

    public required SalesInvoiceStatusDto Status { get; init; }

    public required MoneyDto Total { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
