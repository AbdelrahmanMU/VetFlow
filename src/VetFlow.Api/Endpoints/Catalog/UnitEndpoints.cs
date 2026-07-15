using VetFlow.Application.Catalog.Queries.UnitOptions;
using VetFlow.Application.Common;

namespace VetFlow.Api.Endpoints.Catalog;

public static class UnitEndpoints
{
    public static void MapUnitEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/units",
            async (
                [AsParameters] LookupRequest request,
                IQueryHandler<UnitOptionsQuery, PagedResult<LookupOptionDto>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = request.ToQuery((search, page, pageSize) =>
                    new UnitOptionsQuery { Search = search, Page = page, PageSize = pageSize });
                return Results.Ok(await handler.HandleAsync(query, cancellationToken));
            });
    }
}
