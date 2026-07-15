using VetFlow.Application.Catalog.Queries.ManufacturerOptions;
using VetFlow.Application.Common;

namespace VetFlow.Api.Endpoints.Catalog;

public static class ManufacturerEndpoints
{
    public static void MapManufacturerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/manufacturers",
            async (
                [AsParameters] LookupRequest request,
                IQueryHandler<ManufacturerOptionsQuery, PagedResult<LookupOptionDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = request.ToQuery((search, page, pageSize) =>
                    new ManufacturerOptionsQuery { Search = search, Page = page, PageSize = pageSize });
                return Results.Ok(await handler.HandleAsync(query, cancellationToken));
            });
    }
}
