using VetFlow.Application.Catalog.Queries.ManufacturerList;
using VetFlow.Application.Common;

namespace VetFlow.Api.Endpoints.Catalog;

/// <summary>
/// Raw query-string surface of GET /api/v1/manufacturers — the whitelisted search,
/// sort, and pagination parameters (STD-API-023), parsed explicitly so every
/// malformed value produces the canonical validation shape. A deliberate mirror of
/// the category list request (Categories owns its own version).
/// </summary>
public sealed record ManufacturerListRequest(string? Search, string? Sort, string? Dir, string? Page, string? PageSize)
{
    private static readonly Dictionary<string, ManufacturerListSortField> SortTokens = new()
    {
        ["name"] = ManufacturerListSortField.Name,
        ["status"] = ManufacturerListSortField.Status,
    };

    private static readonly Dictionary<string, SortDirection> DirectionTokens = new()
    {
        ["asc"] = SortDirection.Ascending,
        ["desc"] = SortDirection.Descending,
    };

    public ManufacturerListQuery ToQuery()
    {
        var parser = new QueryStringParser();

        var query = new ManufacturerListQuery
        {
            Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
            Sort = parser.ParseToken(
                Sort, "sort", SortTokens, ManufacturerListSortField.Name, ValidationMessageKeys.UnknownSortField),
            Direction = parser.ParseToken(
                Dir, "dir", DirectionTokens, SortDirection.Ascending, ValidationMessageKeys.UnknownSortDirection),
            Page = parser.ParseInteger(Page, "page", 1),
            PageSize = Math.Min(
                parser.ParseInteger(PageSize, "pageSize", ManufacturerListQuery.DefaultPageSize),
                ManufacturerListQuery.MaxPageSize),
        };

        parser.ThrowIfInvalid();
        return query;
    }
}
