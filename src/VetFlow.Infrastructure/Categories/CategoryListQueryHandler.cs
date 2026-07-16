using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Categories.Queries.CategoryList;
using VetFlow.Application.Common;
using VetFlow.Domain.Categories;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Categories;

/// <summary>
/// Category management list (screen: التصنيفات; REQ-CTG-001). Normalized-Arabic
/// name search (STD-BE-044), whitelisted sorting with a unique final key for
/// stable offset pagination, and the fixed pagination envelope. Projects straight
/// to the DTO — a deliberate CQRS-lite read that bypasses the domain (ADR-0014 §5).
/// </summary>
public sealed class CategoryListQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<CategoryListQuery, PagedResult<CategoryListItemDto>>
{
    private const string LikeEscapeCharacter = "\\";

    public async Task<PagedResult<CategoryListItemDto>> HandleAsync(
        CategoryListQuery query,
        CancellationToken cancellationToken)
    {
        var categories = dbContext.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(ArabicSearchText.Normalize(query.Search.Trim()))}%";
            categories = categories.Where(category =>
                EF.Functions.ILike(
                    EF.Property<string>(category, SearchableText.PropertyName), pattern, LikeEscapeCharacter));
        }

        var totalCount = await categories.CountAsync(cancellationToken);

        var items = await ApplySorting(categories, query)
            .Select(category => new CategoryListItemDto
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
            })
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CategoryListItemDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    private static IOrderedQueryable<Category> ApplySorting(IQueryable<Category> categories, CategoryListQuery query)
    {
        var ascending = query.Direction == SortDirection.Ascending;

        var ordered = query.Sort switch
        {
            // Active first when ascending: IsActive true sorts ahead of false.
            CategoryListSortField.Status => ascending
                ? categories.OrderByDescending(category => category.IsActive)
                : categories.OrderBy(category => category.IsActive),
            _ => ascending
                ? categories.OrderBy(category => category.Name)
                : categories.OrderByDescending(category => category.Name),
        };

        var withNameKey = query.Sort == CategoryListSortField.Name
            ? ordered
            : ordered.ThenBy(category => category.Name);

        // A unique final key gives offset pagination a total order so pages stay
        // stable (names are unique after normalization, but the raw Name can still
        // tie on case/whitespace variants that never persist — the Id key is exact).
        return withNameKey.ThenBy(category => category.Id);
    }

    private static string EscapeLike(string value) =>
        value.Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);
}
