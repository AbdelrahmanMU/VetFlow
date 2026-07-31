using VetFlow.Application.Common;
using VetFlow.Application.Sales.Queries.SalesList;

namespace VetFlow.Api.Endpoints.Sales;

/// <summary>
/// Raw query-string surface of GET /api/v1/sales-invoices — the whitelisted
/// search, status and sale-date-range filters, sort, and pagination parameters
/// (BR-SAL-019, STD-API-023), parsed explicitly so every malformed value
/// produces the canonical validation shape.
/// </summary>
public sealed record SalesListRequest(
    string? Search,
    string? Status,
    string? DateFrom,
    string? DateTo,
    string? Sort,
    string? Dir,
    string? Page,
    string? PageSize)
{
    private static readonly Dictionary<string, SalesListSortField> SortTokens = new()
    {
        ["number"] = SalesListSortField.Number,
        // Keys are matched after lower-casing the incoming value (QueryStringParser),
        // so the multi-word token is stored lower-cased.
        ["saledate"] = SalesListSortField.SaleDate,
        ["customer"] = SalesListSortField.Customer,
        ["status"] = SalesListSortField.Status,
        ["total"] = SalesListSortField.Total,
    };

    private static readonly Dictionary<string, SortDirection> DirectionTokens = new()
    {
        ["asc"] = SortDirection.Ascending,
        ["desc"] = SortDirection.Descending,
    };

    private static readonly Dictionary<string, SalesInvoiceStatusFilter?> StatusTokens = new()
    {
        ["draft"] = SalesInvoiceStatusFilter.Draft,
        ["committed"] = SalesInvoiceStatusFilter.Committed,
    };

    public SalesListQuery ToQuery()
    {
        var parser = new QueryStringParser();

        var query = new SalesListQuery
        {
            Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
            Status = parser.ParseToken(
                Status, "status", StatusTokens, null, ValidationMessageKeys.UnknownStatus),
            SaleDateFrom = parser.ParseDate(DateFrom, "dateFrom"),
            SaleDateTo = parser.ParseDate(DateTo, "dateTo"),
            Sort = parser.ParseToken(
                Sort, "sort", SortTokens, SalesListSortField.SaleDate, ValidationMessageKeys.UnknownSortField),
            Direction = parser.ParseToken(
                Dir, "dir", DirectionTokens, SortDirection.Descending, ValidationMessageKeys.UnknownSortDirection),
            Page = parser.ParseInteger(Page, "page", 1),
            PageSize = Math.Min(
                parser.ParseInteger(PageSize, "pageSize", SalesListQuery.DefaultPageSize),
                SalesListQuery.MaxPageSize),
        };

        parser.ThrowIfInvalid();
        return query;
    }
}
