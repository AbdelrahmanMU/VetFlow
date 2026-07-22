using VetFlow.Application.Purchasing.Commands.ReceivePurchaseInvoice;

namespace VetFlow.Api.Endpoints.Purchasing;

/// <summary>
/// JSON body of POST /api/v1/purchase-invoices/{id}/receive (camelCase, STD-API-032). A pure DTO
/// (STD-API-035): the invoice id comes from the route; the body carries an optional expiry date per
/// line (REQ-PUR-005, BR-PUR-013). Lines with no entry default to no expiry — the handler rejects a
/// missing expiry only for a line whose product requires it (DEC-PUR-009).
/// </summary>
public sealed record ReceivePurchaseInvoiceRequest
{
    public IReadOnlyList<ReceiveLineExpiryRequest>? Lines { get; init; }

    public ReceivePurchaseInvoiceCommand ToCommand(Guid invoiceId) => new()
    {
        InvoiceId = invoiceId,
        LineExpiries = (Lines ?? [])
            .Select(line => new ReceiveLineExpiry { LineId = line.LineId ?? Guid.Empty, ExpiryDate = line.ExpiryDate })
            .ToList(),
    };
}

/// <summary>One line's optional expiry date supplied at receiving.</summary>
public sealed record ReceiveLineExpiryRequest
{
    public Guid? LineId { get; init; }

    public DateOnly? ExpiryDate { get; init; }
}
