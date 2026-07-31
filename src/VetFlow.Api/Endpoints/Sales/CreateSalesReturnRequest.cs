using VetFlow.Application.Sales.Commands.CreateSalesReturn;

namespace VetFlow.Api.Endpoints.Sales;

/// <summary>
/// JSON body of POST /api/v1/sales-returns (camelCase, STD-API-032). A pure DTO — the endpoint
/// exposes no domain entity (STD-API-035). Missing values stay null so the command validator
/// produces the canonical per-field validation shape (STD-API-014, AC-SAL-014).
///
/// <para>There is no customer field (snapshotted from the invoice), no reason field (BR-INV-067) and
/// no amount field (DEC-INV-035) — none of them exist on this document.</para>
/// </summary>
public sealed record CreateSalesReturnRequest
{
    public Guid? SalesInvoiceId { get; init; }

    public DateOnly? ReturnDate { get; init; }

    public string? Notes { get; init; }

    public CreateSalesReturnCommand ToCommand() => new()
    {
        SalesInvoiceId = SalesInvoiceId,
        ReturnDate = ReturnDate,
        Notes = Notes,
    };
}
