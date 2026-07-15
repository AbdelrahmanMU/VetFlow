using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.ManufacturerOptions;
using VetFlow.Application.Common;
using VetFlow.Infrastructure.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Catalog;

public sealed class ManufacturerOptionsQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<ManufacturerOptionsQuery, PagedResult<LookupOptionDto>>
{
    public Task<PagedResult<LookupOptionDto>> HandleAsync(
        ManufacturerOptionsQuery query,
        CancellationToken cancellationToken) =>
        LookupOptionsQueryHandler.HandleAsync(
            dbContext.Manufacturers.AsNoTracking(),
            query,
            manufacturer => new LookupOptionDto { Id = manufacturer.Id, Name = manufacturer.Name },
            cancellationToken);
}
