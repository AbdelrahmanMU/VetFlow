using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Queries.InventoryProjection;

namespace VetFlow.Api.Endpoints.Inventory;

/// <summary>
/// Raw query-string surface of GET /api/v1/inventory — the whitelisted search,
/// category, "expiring soon" and "out of stock" filters, sort, and pagination
/// parameters (BR-INV-014, STD-API-023), parsed explicitly so every malformed
/// value produces the canonical validation shape.
/// </summary>
public sealed record InventoryProjectionRequest(
    string? Search,
    string? Category,
    string? ExpiringSoon,
    string? OutOfStock,
    string? Sort,
    string? Dir,
    string? Page,
    string? PageSize)
{
    private static readonly Dictionary<string, InventoryProjectionSortField> SortTokens = new()
    {
        ["product"] = InventoryProjectionSortField.Product,
        // Keys are matched after lower-casing the incoming value (QueryStringParser),
        // so the multi-word tokens are stored lower-cased.
        ["onhand"] = InventoryProjectionSortField.OnHand,
        ["nearestexpiry"] = InventoryProjectionSortField.NearestExpiry,
    };

    private static readonly Dictionary<string, SortDirection> DirectionTokens = new()
    {
        ["asc"] = SortDirection.Ascending,
        ["desc"] = SortDirection.Descending,
    };

    public InventoryProjectionQuery ToQuery()
    {
        var parser = new QueryStringParser();

        var query = new InventoryProjectionQuery
        {
            Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
            CategoryId = parser.ParseId(Category, "category"),
            ExpiringSoon = parser.ParseBoolean(ExpiringSoon, "expiringSoon"),
            OutOfStock = parser.ParseBoolean(OutOfStock, "outOfStock"),
            Sort = parser.ParseToken(
                Sort, "sort", SortTokens, InventoryProjectionSortField.Product, ValidationMessageKeys.UnknownSortField),
            Direction = parser.ParseToken(
                Dir, "dir", DirectionTokens, SortDirection.Ascending, ValidationMessageKeys.UnknownSortDirection),
            Page = parser.ParseInteger(Page, "page", 1),
            PageSize = Math.Min(
                parser.ParseInteger(PageSize, "pageSize", InventoryProjectionQuery.DefaultPageSize),
                InventoryProjectionQuery.MaxPageSize),
        };

        parser.ThrowIfInvalid();
        return query;
    }
}
