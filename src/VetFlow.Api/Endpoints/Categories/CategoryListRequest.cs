using VetFlow.Application.Categories.Queries.CategoryList;
using VetFlow.Application.Common;

namespace VetFlow.Api.Endpoints.Categories;

/// <summary>
/// Raw query-string surface of GET /api/v1/categories — the whitelisted search,
/// sort, and pagination parameters (STD-API-023), parsed explicitly so every
/// malformed value produces the canonical validation shape.
/// </summary>
public sealed record CategoryListRequest(string? Search, string? Sort, string? Dir, string? Page, string? PageSize)
{
    private static readonly Dictionary<string, CategoryListSortField> SortTokens = new()
    {
        ["name"] = CategoryListSortField.Name,
        ["status"] = CategoryListSortField.Status,
    };

    private static readonly Dictionary<string, SortDirection> DirectionTokens = new()
    {
        ["asc"] = SortDirection.Ascending,
        ["desc"] = SortDirection.Descending,
    };

    public CategoryListQuery ToQuery()
    {
        var parser = new QueryStringParser();

        var query = new CategoryListQuery
        {
            Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
            Sort = parser.ParseToken(
                Sort, "sort", SortTokens, CategoryListSortField.Name, ValidationMessageKeys.UnknownSortField),
            Direction = parser.ParseToken(
                Dir, "dir", DirectionTokens, SortDirection.Ascending, ValidationMessageKeys.UnknownSortDirection),
            Page = parser.ParseInteger(Page, "page", 1),
            PageSize = Math.Min(
                parser.ParseInteger(PageSize, "pageSize", CategoryListQuery.DefaultPageSize),
                CategoryListQuery.MaxPageSize),
        };

        parser.ThrowIfInvalid();
        return query;
    }
}
