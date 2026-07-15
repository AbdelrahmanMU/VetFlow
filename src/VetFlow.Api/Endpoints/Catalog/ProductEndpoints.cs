using VetFlow.Application.Catalog.Queries.ProductList;
using VetFlow.Application.Common;

namespace VetFlow.Api.Endpoints.Catalog;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/products",
            async (
                [AsParameters] ProductListRequest request,
                IQueryHandler<ProductListQuery, PagedResult<ProductListItemDto>> handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(request.ToQuery(), cancellationToken)));
    }
}
