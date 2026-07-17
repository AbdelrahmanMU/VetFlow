using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Queries.PurchaseList;

namespace VetFlow.Application.Purchasing.Queries.PurchaseDetails;

/// <summary>
/// The full read model of one purchase invoice (REQ-PUR-002, screen "تفاصيل فاتورة
/// الشراء"): the complete header (BR-PUR-001) — system number, supplier name, the
/// optional supplier reference, invoice date, status, total, optional notes, and
/// the creation timestamp. Read-only; line items, cost, receiving, and the
/// inventory effect belong to later slices (scope lock, overview.md).
/// </summary>
public sealed record PurchaseDetailsDto
{
    public required Guid Id { get; init; }

    public required string Number { get; init; }

    public required string SupplierName { get; init; }

    public string? SupplierInvoiceReference { get; init; }

    public required DateOnly InvoiceDate { get; init; }

    public required PurchaseInvoiceStatusDto Status { get; init; }

    public required MoneyDto Total { get; init; }

    public string? Notes { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
