using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Categories.Queries.CategoryOptions;
using VetFlow.Application.Common;
using VetFlow.Infrastructure.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Categories;

public sealed class CategoryOptionsQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<CategoryOptionsQuery, PagedResult<LookupOptionDto>>
{
    public Task<PagedResult<LookupOptionDto>> HandleAsync(
        CategoryOptionsQuery query,
        CancellationToken cancellationToken) =>
        LookupOptionsQueryHandler.HandleAsync(
            dbContext.Categories.AsNoTracking(),
            query,
            category => new LookupOptionDto { Id = category.Id, Name = category.Name },
            cancellationToken);
}
