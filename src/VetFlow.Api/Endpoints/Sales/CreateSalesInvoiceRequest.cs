using VetFlow.Application.Sales.Commands.CreateSalesInvoice;

namespace VetFlow.Api.Endpoints.Sales;

/// <summary>
/// JSON body of POST /api/v1/sales-invoices (camelCase, STD-API-032). A pure DTO — the endpoint
/// exposes no domain entity (STD-API-035). Missing values map to their empty form so the command
/// validator produces the canonical per-field validation shape (STD-API-014, AC-SAL-002); the
/// customer name is genuinely optional (DEC-SAL-002), so its absence is not an error.
/// </summary>
public sealed record CreateSalesInvoiceRequest
{
    public string? CustomerName { get; init; }

    public DateOnly? SaleDate { get; init; }

    public string? Notes { get; init; }

    public CreateSalesInvoiceCommand ToCommand() => new()
    {
        CustomerName = CustomerName,
        SaleDate = SaleDate,
        Notes = Notes,
    };
}
