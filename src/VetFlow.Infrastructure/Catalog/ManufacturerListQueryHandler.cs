using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.ManufacturerList;
using VetFlow.Application.Common;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// Manufacturer management list (screen: الشركات المصنعة; REQ-CAT-047). Normalized-
/// Arabic name search (STD-BE-044), whitelisted sorting with a unique final key for
/// stable offset pagination, and the fixed pagination envelope. Projects straight to
/// the DTO — a deliberate CQRS-lite read that bypasses the domain (ADR-0014 §5). A
/// deliberate copy of the category list handler (Categories owns its own version).
/// </summary>
public sealed class ManufacturerListQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<ManufacturerListQuery, PagedResult<ManufacturerListItemDto>>
{
    private const string LikeEscapeCharacter = "\\";

    public async Task<PagedResult<ManufacturerListItemDto>> HandleAsync(
        ManufacturerListQuery query,
        CancellationToken cancellationToken)
    {
        var manufacturers = dbContext.Manufacturers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(ArabicSearchText.Normalize(query.Search.Trim()))}%";
            manufacturers = manufacturers.Where(manufacturer =>
                EF.Functions.ILike(
                    EF.Property<string>(manufacturer, SearchableText.PropertyName), pattern, LikeEscapeCharacter));
        }

        var totalCount = await manufacturers.CountAsync(cancellationToken);

        var items = await ApplySorting(manufacturers, query)
            .Select(manufacturer => new ManufacturerListItemDto
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                IsActive = manufacturer.IsActive,
            })
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ManufacturerListItemDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    private static IOrderedQueryable<Manufacturer> ApplySorting(
        IQueryable<Manufacturer> manufacturers,
        ManufacturerListQuery query)
    {
        var ascending = query.Direction == SortDirection.Ascending;

        var ordered = query.Sort switch
        {
            // Active first when ascending: IsActive true sorts ahead of false.
            ManufacturerListSortField.Status => ascending
                ? manufacturers.OrderByDescending(manufacturer => manufacturer.IsActive)
                : manufacturers.OrderBy(manufacturer => manufacturer.IsActive),
            _ => ascending
                ? manufacturers.OrderBy(manufacturer => manufacturer.Name)
                : manufacturers.OrderByDescending(manufacturer => manufacturer.Name),
        };

        var withNameKey = query.Sort == ManufacturerListSortField.Name
            ? ordered
            : ordered.ThenBy(manufacturer => manufacturer.Name);

        // A unique final key gives offset pagination a total order so pages stay
        // stable (names are unique after normalization, but the raw Name can still
        // tie on case/whitespace variants that never persist — the Id key is exact).
        return withNameKey.ThenBy(manufacturer => manufacturer.Id);
    }

    private static string EscapeLike(string value) =>
        value.Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);
}
