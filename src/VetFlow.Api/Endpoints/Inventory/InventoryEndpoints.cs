using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Queries.InventoryProjection;

namespace VetFlow.Api.Endpoints.Inventory;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        // The inventory projection — a read-only view of the current physical inventory
        // (REQ-INV-002). Read-only: no create/update/delete surface (BR-INV-006).
        app.MapGet(
            "/api/v1/inventory",
            async (
                [AsParameters] InventoryProjectionRequest request,
                IQueryHandler<InventoryProjectionQuery, PagedResult<InventoryProjectionItemDto>> handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(request.ToQuery(), cancellationToken)));
    }
}
