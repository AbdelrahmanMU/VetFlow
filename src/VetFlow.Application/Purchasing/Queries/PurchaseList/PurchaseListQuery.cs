using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Queries.PurchaseList;

/// <summary>
/// The purchase list (REQ-PUR-001): search by system number, supplier name, or
/// the supplier's invoice reference (BR-PUR-004 — never notes), the status and
/// invoice-date-range filters, whitelisted sorting, and offset pagination
/// (ADR-0015 §5). The default order is the most recent invoice first —
/// invoice date descending (BR-PUR-004), mirroring the Catalog list pattern.
/// </summary>
public sealed record PurchaseListQuery : IQuery<PagedResult<PurchaseListItemDto>>
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 200;

    /// <summary>
    /// Upper bound on the page number. Derived so the handler's Int32 offset
    /// <c>(Page - 1) * PageSize</c> can never overflow (STD-BE-027): with
    /// <see cref="MaxPageSize"/> capped, the largest offset stays below Int32.MaxValue.
    /// </summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;

    public string? Search { get; init; }

    public PurchaseInvoiceStatusFilter? Status { get; init; }

    public DateOnly? InvoiceDateFrom { get; init; }

    public DateOnly? InvoiceDateTo { get; init; }

    public PurchaseListSortField Sort { get; init; } = PurchaseListSortField.InvoiceDate;

    public SortDirection Direction { get; init; } = SortDirection.Descending;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}
