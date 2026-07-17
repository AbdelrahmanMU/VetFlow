using VetFlow.Application.Purchasing.Commands.CreatePurchaseInvoice;

namespace VetFlow.Api.Endpoints.Purchasing;

/// <summary>
/// JSON body of POST /api/v1/purchase-invoices (camelCase, STD-API-032). A pure
/// DTO — the endpoint exposes no domain entity (STD-API-035). Missing values map
/// to their empty form so the command validator produces the canonical per-field
/// validation shape (STD-API-014, AC-PUR-007).
/// </summary>
public sealed record CreatePurchaseInvoiceRequest
{
    public string? SupplierName { get; init; }

    public string? SupplierInvoiceReference { get; init; }

    public DateOnly? InvoiceDate { get; init; }

    public string? Notes { get; init; }

    public CreatePurchaseInvoiceCommand ToCommand() => new()
    {
        SupplierName = SupplierName ?? string.Empty,
        SupplierInvoiceReference = SupplierInvoiceReference,
        InvoiceDate = InvoiceDate,
        Notes = Notes,
    };
}
