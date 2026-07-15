using VetFlow.Application.Catalog.Queries.ProductNatureOptions;
using VetFlow.Application.Common;

namespace VetFlow.Api.Endpoints.Catalog;

public static class ProductNatureEndpoints
{
    public static void MapProductNatureEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/product-natures",
            async (
                [AsParameters] LookupRequest request,
                IQueryHandler<ProductNatureOptionsQuery, PagedResult<LookupOptionDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = request.ToQuery((search, page, pageSize) =>
                    new ProductNatureOptionsQuery { Search = search, Page = page, PageSize = pageSize });
                return Results.Ok(await handler.HandleAsync(query, cancellationToken));
            });
    }
}
