using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Queries.PurchaseList;

/// <summary>
/// One row of the purchase list (REQ-PUR-001): the six frozen columns — system
/// number, supplier name, invoice date, status, total, and creation timestamp
/// (BR-PUR-004). No stock, no line items (scope lock).
/// </summary>
public sealed record PurchaseListItemDto
{
    public required Guid Id { get; init; }

    public required string Number { get; init; }

    public required string SupplierName { get; init; }

    public string? SupplierInvoiceReference { get; init; }

    public required DateOnly InvoiceDate { get; init; }

    public required PurchaseInvoiceStatusDto Status { get; init; }

    public required MoneyDto Total { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
