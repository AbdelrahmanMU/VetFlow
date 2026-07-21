using VetFlow.Application.Purchasing.Commands.AddPurchaseLineItem;

namespace VetFlow.Api.Endpoints.Purchasing;

/// <summary>
/// JSON body of POST /api/v1/purchase-invoices/{id}/lines (camelCase, STD-API-032). A
/// pure DTO — the endpoint exposes no domain entity (STD-API-035). The invoice id comes
/// from the route; the product, purchase unit, quantity, and unit price come from the
/// body. Missing values map to their empty form so the command validator produces the
/// canonical per-field validation shape (STD-API-014, AC-PUR-009).
/// </summary>
public sealed record AddPurchaseLineItemRequest
{
    public Guid? ProductId { get; init; }

    public Guid? PurchaseUnitId { get; init; }

    public decimal? Quantity { get; init; }

    public decimal? UnitPrice { get; init; }

    public AddPurchaseLineItemCommand ToCommand(Guid invoiceId) => new()
    {
        InvoiceId = invoiceId,
        ProductId = ProductId ?? Guid.Empty,
        PurchaseUnitId = PurchaseUnitId ?? Guid.Empty,
        Quantity = Quantity ?? 0m,
        UnitPrice = UnitPrice ?? 0m,
    };
}
