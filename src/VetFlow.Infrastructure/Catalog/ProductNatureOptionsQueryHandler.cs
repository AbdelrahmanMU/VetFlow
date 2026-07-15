using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.ProductNatureOptions;
using VetFlow.Application.Common;
using VetFlow.Infrastructure.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Catalog;

public sealed class ProductNatureOptionsQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<ProductNatureOptionsQuery, PagedResult<LookupOptionDto>>
{
    public Task<PagedResult<LookupOptionDto>> HandleAsync(
        ProductNatureOptionsQuery query,
        CancellationToken cancellationToken) =>
        LookupOptionsQueryHandler.HandleAsync(
            dbContext.ProductNatures.AsNoTracking(),
            query,
            nature => new LookupOptionDto { Id = nature.Id, Name = nature.Name },
            cancellationToken);
}
