using VetFlow.Application.Categories.Queries.CategoryOptions;
using VetFlow.Application.Common;

namespace VetFlow.Api.Endpoints.Categories;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/categories",
            async (
                [AsParameters] LookupRequest request,
                IQueryHandler<CategoryOptionsQuery, PagedResult<LookupOptionDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = request.ToQuery((search, page, pageSize) =>
                    new CategoryOptionsQuery { Search = search, Page = page, PageSize = pageSize });
                return Results.Ok(await handler.HandleAsync(query, cancellationToken));
            });
    }
}
