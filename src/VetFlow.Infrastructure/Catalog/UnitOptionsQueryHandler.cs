using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.UnitOptions;
using VetFlow.Application.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// Managed-unit options for the unit-profile editor (REQ-CAT-016). Units are a
/// small controlled list with no normalized search column, so an optional plain
/// name filter suffices; ordering and the fixed envelope match the other lookups.
/// </summary>
public sealed class UnitOptionsQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<UnitOptionsQuery, PagedResult<LookupOptionDto>>
{
    public async Task<PagedResult<LookupOptionDto>> HandleAsync(
        UnitOptionsQuery query,
        CancellationToken cancellationToken)
    {
        var units = dbContext.Units.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            units = units.Where(unit => EF.Functions.ILike(unit.Name, pattern));
        }

        var options = units
            .Select(unit => new LookupOptionDto { Id = unit.Id, Name = unit.Name })
            .OrderBy(option => option.Name);

        var totalCount = await options.CountAsync(cancellationToken);
        var items = await options
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LookupOptionDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }
}
