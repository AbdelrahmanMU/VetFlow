using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Queries.BatchViewer;
using VetFlow.Application.Inventory.Queries.ExpiryMonitoring;
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

        // Expiry monitoring — a read-only, clinic-wide list of active batches with a real
        // expiry that are expired or expiring soon (REQ-INV-004). Read-only, no alerts/actions
        // (BR-INV-032). Registered before the {productId} batch route (distinct path anyway).
        app.MapGet(
            "/api/v1/inventory/expiry",
            async (
                [AsParameters] ExpiryMonitoringRequest request,
                IQueryHandler<ExpiryMonitoringQuery, PagedResult<ExpiryMonitoringItemDto>> handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(request.ToQuery(), cancellationToken)));

        // The batch viewer — a read-only per-product view of every inventory batch
        // (REQ-INV-003). A null result means the product does not exist → 404 (AC-INV-022).
        // Read-only: no write surface (BR-INV-018).
        app.MapGet(
            "/api/v1/inventory/{productId:guid}/batches",
            async (
                Guid productId,
                [AsParameters] BatchViewerRequest request,
                IQueryHandler<BatchViewerQuery, BatchViewerResult?> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request.ToQuery(productId), cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            });
    }
}
