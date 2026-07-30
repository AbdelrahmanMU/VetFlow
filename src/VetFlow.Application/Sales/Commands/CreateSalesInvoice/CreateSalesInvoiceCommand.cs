using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.CreateSalesInvoice;

/// <summary>
/// Create a sales invoice (REQ-SAL-001): the header only — the <b>optional</b> free-text customer
/// name (DEC-SAL-002), the required sale date, and optional notes (BR-SAL-001). The system number
/// (BR-SAL-002) is generated at persist time and is never supplied here; the invoice is always born
/// a <see cref="Domain.Sales.SalesInvoiceStatus.Draft"/> (BR-SAL-003) with a zero total, and a draft
/// has <b>no inventory effect at all</b> — no reservation, no decrement (BR-SAL-004/010).
/// </summary>
public sealed record CreateSalesInvoiceCommand : ICommand<CreateSalesInvoiceResult>
{
    /// <summary>Optional (DEC-SAL-002) — direct cash sales may have no named customer.</summary>
    public string? CustomerName { get; init; }

    /// <summary>Required; validated <c>NotNull</c> so a missing date is a per-field 400 (AC-SAL-002).</summary>
    public DateOnly? SaleDate { get; init; }

    public string? Notes { get; init; }
}
