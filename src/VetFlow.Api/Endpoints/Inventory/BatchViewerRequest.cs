using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Queries.BatchViewer;

namespace VetFlow.Api.Endpoints.Inventory;

/// <summary>
/// Raw query-string surface of GET /api/v1/inventory/{productId}/batches — the whitelisted
/// batch-status, "expired" and "expiring soon" filters, sort, and pagination parameters
/// (BR-INV-026/027, STD-API-023), parsed explicitly so every malformed value produces the
/// canonical validation shape. The product id comes from the route.
/// </summary>
public sealed record BatchViewerRequest(
    string? Status,
    string? Expired,
    string? ExpiringSoon,
    string? Sort,
    string? Dir,
    string? Page,
    string? PageSize)
{
    private static readonly Dictionary<string, BatchStatus?> StatusTokens = new()
    {
        ["active"] = BatchStatus.Active,
        ["depleted"] = BatchStatus.Depleted,
    };

    private static readonly Dictionary<string, BatchViewerSortField> SortTokens = new()
    {
        // Keys are matched after lower-casing the incoming value (QueryStringParser),
        // so the multi-word tokens are stored lower-cased.
        ["receivedate"] = BatchViewerSortField.ReceiveDate,
        ["expirydate"] = BatchViewerSortField.ExpiryDate,
        ["remainingquantity"] = BatchViewerSortField.RemainingQuantity,
    };

    private static readonly Dictionary<string, SortDirection> DirectionTokens = new()
    {
        ["asc"] = SortDirection.Ascending,
        ["desc"] = SortDirection.Descending,
    };

    public BatchViewerQuery ToQuery(Guid productId)
    {
        var parser = new QueryStringParser();

        var query = new BatchViewerQuery
        {
            ProductId = productId,
            Status = parser.ParseToken<BatchStatus?>(
                Status, "status", StatusTokens, null, ValidationMessageKeys.UnknownStatus),
            Expired = parser.ParseBoolean(Expired, "expired"),
            ExpiringSoon = parser.ParseBoolean(ExpiringSoon, "expiringSoon"),
            Sort = parser.ParseToken(
                Sort, "sort", SortTokens, BatchViewerSortField.ReceiveDate, ValidationMessageKeys.UnknownSortField),
            Direction = parser.ParseToken(
                Dir, "dir", DirectionTokens, SortDirection.Descending, ValidationMessageKeys.UnknownSortDirection),
            Page = parser.ParseInteger(Page, "page", 1),
            PageSize = Math.Min(
                parser.ParseInteger(PageSize, "pageSize", BatchViewerQuery.DefaultPageSize),
                BatchViewerQuery.MaxPageSize),
        };

        parser.ThrowIfInvalid();
        return query;
    }
}
