using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Queries.SalesDetails;

/// <summary>
/// The full read model of one sales invoice (REQ-SAL-002, screen "تفاصيل فاتورة البيع"): the
/// complete header (BR-SAL-001) — system number, the optional customer name, sale date, status,
/// derived total, optional notes, and the creation timestamp — in the frozen canonical order
/// (BR-SAL-008). Read-only. It exposes <b>no batch information whatsoever</b>, neither before nor
/// after the commit (BR-SAL-013).
/// </summary>
public sealed record SalesDetailsDto
{
    public required Guid Id { get; init; }

    public required string Number { get; init; }

    /// <summary>Optional (DEC-SAL-002); the UI shows the standard placeholder when absent.</summary>
    public string? CustomerName { get; init; }

    public required DateOnly SaleDate { get; init; }

    public required SalesInvoiceStatusDto Status { get; init; }

    public required MoneyDto Total { get; init; }

    public string? Notes { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
