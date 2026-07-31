using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Queries.SalesList;

/// <summary>
/// The basic sales list (REQ-SAL-005, DEC-SAL-005 — owner-ruled 2026-07-31):
/// search by system number or customer name (BR-SAL-019 — never notes), the
/// status and sale-date-range filters, whitelisted sorting, and offset
/// pagination (ADR-0015 §5). Mirrors PurchaseListQuery with the sales-header
/// differences only: customer instead of supplier, sale date instead of
/// invoice date, two statuses instead of three, and no external reference.
/// The default order is the most recent sale first — sale date descending
/// (BR-SAL-019, the BR-PUR-004 pattern).
/// </summary>
public sealed record SalesListQuery : IQuery<PagedResult<SalesListItemDto>>
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

    public SalesInvoiceStatusFilter? Status { get; init; }

    public DateOnly? SaleDateFrom { get; init; }

    public DateOnly? SaleDateTo { get; init; }

    public SalesListSortField Sort { get; init; } = SalesListSortField.SaleDate;

    public SortDirection Direction { get; init; } = SortDirection.Descending;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}
