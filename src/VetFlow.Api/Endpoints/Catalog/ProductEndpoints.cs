using VetFlow.Application.Catalog.Commands.CreateProduct;
using VetFlow.Application.Catalog.Queries.PossibleDuplicates;
using VetFlow.Application.Catalog.Queries.ProductDetails;
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

        // The literal segment is matched ahead of the {id:guid} route below.
        app.MapGet(
            "/api/v1/products/possible-duplicates",
            async (
                [AsParameters] PossibleDuplicatesRequest request,
                IQueryHandler<PossibleDuplicatesQuery, PagedResult<PossibleDuplicateDto>> handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(request.ToQuery(), cancellationToken)));

        app.MapGet(
            "/api/v1/products/{id:guid}",
            async (
                Guid id,
                IQueryHandler<ProductDetailsQuery, ProductDetailsDto?> handler,
                CancellationToken cancellationToken) =>
            {
                var details = await handler.HandleAsync(new ProductDetailsQuery { Id = id }, cancellationToken);
                return details is null ? Results.NotFound() : Results.Ok(details);
            });

        app.MapPost(
            "/api/v1/products",
            async (
                CreateProductRequest request,
                ICommandHandler<CreateProductCommand, CreateProductResult> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
                return Results.Created($"/api/v1/products/{result.Id}", result);
            });
    }
}
