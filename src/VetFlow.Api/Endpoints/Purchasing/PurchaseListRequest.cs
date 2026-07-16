using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Queries.PurchaseList;

namespace VetFlow.Api.Endpoints.Purchasing;

/// <summary>
/// Raw query-string surface of GET /api/v1/purchase-invoices — the whitelisted
/// search, status and invoice-date-range filters, sort, and pagination
/// parameters (BR-PUR-004, STD-API-023), parsed explicitly so every malformed
/// value produces the canonical validation shape.
/// </summary>
public sealed record PurchaseListRequest(
    string? Search,
    string? Status,
    string? DateFrom,
    string? DateTo,
    string? Sort,
    string? Dir,
    string? Page,
    string? PageSize)
{
    private static readonly Dictionary<string, PurchaseListSortField> SortTokens = new()
    {
        ["number"] = PurchaseListSortField.Number,
        // Keys are matched after lower-casing the incoming value (QueryStringParser),
        // so the multi-word token is stored lower-cased.
        ["invoicedate"] = PurchaseListSortField.InvoiceDate,
        ["supplier"] = PurchaseListSortField.Supplier,
        ["status"] = PurchaseListSortField.Status,
        ["total"] = PurchaseListSortField.Total,
    };

    private static readonly Dictionary<string, SortDirection> DirectionTokens = new()
    {
        ["asc"] = SortDirection.Ascending,
        ["desc"] = SortDirection.Descending,
    };

    private static readonly Dictionary<string, PurchaseInvoiceStatusFilter?> StatusTokens = new()
    {
        ["draft"] = PurchaseInvoiceStatusFilter.Draft,
        ["received"] = PurchaseInvoiceStatusFilter.Received,
        ["cancelled"] = PurchaseInvoiceStatusFilter.Cancelled,
    };

    public PurchaseListQuery ToQuery()
    {
        var parser = new QueryStringParser();

        var query = new PurchaseListQuery
        {
            Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
            Status = parser.ParseToken(
                Status, "status", StatusTokens, null, ValidationMessageKeys.UnknownStatus),
            InvoiceDateFrom = parser.ParseDate(DateFrom, "dateFrom"),
            InvoiceDateTo = parser.ParseDate(DateTo, "dateTo"),
            Sort = parser.ParseToken(
                Sort, "sort", SortTokens, PurchaseListSortField.InvoiceDate, ValidationMessageKeys.UnknownSortField),
            Direction = parser.ParseToken(
                Dir, "dir", DirectionTokens, SortDirection.Descending, ValidationMessageKeys.UnknownSortDirection),
            Page = parser.ParseInteger(Page, "page", 1),
            PageSize = Math.Min(
                parser.ParseInteger(PageSize, "pageSize", PurchaseListQuery.DefaultPageSize),
                PurchaseListQuery.MaxPageSize),
        };

        parser.ThrowIfInvalid();
        return query;
    }
}
