using VetFlow.Application.Sales.Commands.AddSalesLineItem;

namespace VetFlow.Api.Endpoints.Sales;

/// <summary>
/// JSON body of POST /api/v1/sales-invoices/{id}/lines (camelCase, STD-API-032). A pure DTO — the
/// endpoint exposes no domain entity (STD-API-035). The invoice id comes from the route; the
/// product, sale unit, and quantity come from the body. There is deliberately <b>no price
/// field</b>: the unit price is the catalog snapshot taken by the handler and is not client input
/// (DEC-SAL-003). Missing values map to their empty form so the command validator produces the
/// canonical per-field validation shape (STD-API-014, AC-SAL-003).
/// </summary>
public sealed record AddSalesLineItemRequest
{
    public Guid? ProductId { get; init; }

    public Guid? SaleUnitId { get; init; }

    public decimal? Quantity { get; init; }

    public AddSalesLineItemCommand ToCommand(Guid invoiceId) => new()
    {
        InvoiceId = invoiceId,
        ProductId = ProductId ?? Guid.Empty,
        SaleUnitId = SaleUnitId ?? Guid.Empty,
        Quantity = Quantity ?? 0m,
    };
}
