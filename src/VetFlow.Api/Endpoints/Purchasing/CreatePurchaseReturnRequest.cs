using VetFlow.Application.Purchasing.Commands.CreatePurchaseReturn;

namespace VetFlow.Api.Endpoints.Purchasing;

/// <summary>
/// JSON body of POST /api/v1/purchase-returns (camelCase, STD-API-032). A pure DTO — the endpoint
/// exposes no domain entity (STD-API-035). Missing values stay null so the command validator
/// produces the canonical per-field validation shape (STD-API-014, AC-PUR-019).
///
/// <para>There is no supplier field (snapshotted from the invoice), no reason field
/// (BR-INV-067) and no amount field (DEC-INV-035) — none of them exist on this document.</para>
/// </summary>
public sealed record CreatePurchaseReturnRequest
{
    public Guid? PurchaseInvoiceId { get; init; }

    public DateOnly? ReturnDate { get; init; }

    public string? Notes { get; init; }

    public CreatePurchaseReturnCommand ToCommand() => new()
    {
        PurchaseInvoiceId = PurchaseInvoiceId,
        ReturnDate = ReturnDate,
        Notes = Notes,
    };
}
