using VetFlow.Application.Inventory.Queries.InventoryHistory;

namespace VetFlow.Api.Endpoints.Inventory;

/// <summary>
/// Raw query-string surface of GET /api/v1/inventory/movements — pagination only, parsed
/// explicitly so every malformed value produces the canonical validation shape (STD-API-023).
/// There is no search, no filter and no user-selectable sort: the history is an unfiltered
/// chronological list in this slice, newest first (BR-INV-044).
/// </summary>
public sealed record InventoryHistoryRequest(string? Page, string? PageSize)
{
    public InventoryHistoryQuery ToQuery()
    {
        var parser = new QueryStringParser();

        var query = new InventoryHistoryQuery
        {
            Page = parser.ParseInteger(Page, "page", 1),
            PageSize = Math.Min(
                parser.ParseInteger(PageSize, "pageSize", InventoryHistoryQuery.DefaultPageSize),
                InventoryHistoryQuery.MaxPageSize),
        };

        parser.ThrowIfInvalid();
        return query;
    }
}
