using VetFlow.Application.Catalog.Queries.ProductList;
using VetFlow.Application.Common;

namespace VetFlow.Api.Endpoints.Catalog;

/// <summary>
/// Raw query-string surface of GET /api/v1/products — the whitelisted filter,
/// sort, and pagination parameters (STD-API-023), parsed explicitly so every
/// malformed value produces the canonical validation shape.
/// </summary>
public sealed record ProductListRequest(
    string? Search,
    string? CategoryId,
    string? ManufacturerId,
    string? NatureId,
    string? Status,
    string? Refrigerated,
    string? HasExpiration,
    string? Splittable,
    string? HasSellingPrice,
    string? Sort,
    string? Dir,
    string? Page,
    string? PageSize)
{
    private static readonly Dictionary<string, ProductListSortField> SortTokens = new()
    {
        ["name"] = ProductListSortField.Name,
        ["category"] = ProductListSortField.Category,
        ["manufacturer"] = ProductListSortField.Manufacturer,
        ["nature"] = ProductListSortField.Nature,
        ["price"] = ProductListSortField.Price,
        ["status"] = ProductListSortField.Status,
    };

    private static readonly Dictionary<string, SortDirection> DirectionTokens = new()
    {
        ["asc"] = SortDirection.Ascending,
        ["desc"] = SortDirection.Descending,
    };

    private static readonly Dictionary<string, ProductStatusFilter?> StatusTokens = new()
    {
        ["active"] = ProductStatusFilter.Active,
        ["disabled"] = ProductStatusFilter.Disabled,
    };

    public ProductListQuery ToQuery()
    {
        var parser = new QueryStringParser();

        var query = new ProductListQuery
        {
            Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
            CategoryId = parser.ParseId(CategoryId, "categoryId"),
            ManufacturerId = parser.ParseId(ManufacturerId, "manufacturerId"),
            NatureId = parser.ParseId(NatureId, "natureId"),
            Status = parser.ParseToken(
                Status, "status", StatusTokens, null, ValidationMessageKeys.UnknownStatus),
            IsRefrigerated = parser.ParseBoolean(Refrigerated, "refrigerated"),
            HasExpiration = parser.ParseBoolean(HasExpiration, "hasExpiration"),
            IsSplittable = parser.ParseBoolean(Splittable, "splittable"),
            HasSellingPrice = parser.ParseBoolean(HasSellingPrice, "hasSellingPrice"),
            Sort = parser.ParseToken(
                Sort, "sort", SortTokens, ProductListSortField.Name, ValidationMessageKeys.UnknownSortField),
            Direction = parser.ParseToken(
                Dir, "dir", DirectionTokens, SortDirection.Ascending, ValidationMessageKeys.UnknownSortDirection),
            Page = parser.ParseInteger(Page, "page", 1),
            PageSize = Math.Min(
                parser.ParseInteger(PageSize, "pageSize", ProductListQuery.DefaultPageSize),
                ProductListQuery.MaxPageSize),
        };

        parser.ThrowIfInvalid();
        return query;
    }
}
