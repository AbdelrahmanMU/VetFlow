using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Queries.SalesLineItems;

/// <summary>
/// One line of a sales invoice in the read model (REQ-SAL-002, section "بنود فاتورة البيع"): the
/// product and sale-unit name snapshots and the unit-price snapshot (BR-SAL-006), the quantity, and
/// the line total (BR-SAL-004). The referenced ids are carried for the UI; display uses the
/// snapshots. <b>No batch column, no allocation detail, no expiry</b> (BR-SAL-013).
/// </summary>
public sealed record SalesLineItemDto
{
    public required Guid Id { get; init; }

    public required Guid ProductId { get; init; }

    public required string ProductName { get; init; }

    public required Guid SaleUnitId { get; init; }

    public required string SaleUnitName { get; init; }

    public required decimal Quantity { get; init; }

    public required MoneyDto UnitPrice { get; init; }

    public required MoneyDto LineTotal { get; init; }
}
