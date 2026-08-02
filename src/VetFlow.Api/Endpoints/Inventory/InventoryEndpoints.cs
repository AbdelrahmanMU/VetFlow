using VetFlow.Application.Common;
using VetFlow.Application.Inventory.Commands.AdjustInventory;
using VetFlow.Application.Inventory.Commands.WriteOffInventory;
using VetFlow.Application.Inventory.Queries.BatchViewer;
using VetFlow.Application.Inventory.Queries.ExpiryMonitoring;
using VetFlow.Application.Inventory.Queries.InventoryHistory;
using VetFlow.Application.Inventory.Queries.InventoryProjection;
using VetFlow.Application.Inventory.Queries.ProductInventorySummary;

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

        // Inventory adjustment (REQ-INV-010) — the first Inventory-owned write surface. A null
        // result means the batch does not exist → 404. Business rejections travel as error codes:
        // VTF-INV-061 (would go below zero), VTF-INV-067 (reason not in the adjustment list),
        // VTF-INV-068 (the batch changed while saving).
        app.MapPost(
            "/api/v1/inventory/adjustments",
            async (
                AdjustInventoryRequest request,
                ICommandHandler<AdjustInventoryCommand, Guid?> handler,
                CancellationToken cancellationToken) =>
            {
                var movementId = await handler.HandleAsync(request.ToCommand(), cancellationToken);
                return movementId is null
                    ? Results.NotFound()
                    // The movement is readable in the history collection; there is no per-movement
                    // resource, and pointing at one that does not exist would be a broken link.
                    : Results.Created("/api/v1/inventory/movements", new { movementId });
            });

        // Write-off (REQ-INV-011) — the exit path R9 asked for. Same rejections as an adjustment,
        // and an expired batch is deliberately eligible: DEC-INV-021 keeps expired stock out of
        // sales allocation, not out of disposal.
        app.MapPost(
            "/api/v1/inventory/write-offs",
            async (
                WriteOffInventoryRequest request,
                ICommandHandler<WriteOffInventoryCommand, Guid?> handler,
                CancellationToken cancellationToken) =>
            {
                var movementId = await handler.HandleAsync(request.ToCommand(), cancellationToken);
                return movementId is null
                    ? Results.NotFound()
                    : Results.Created("/api/v1/inventory/movements", new { movementId });
            });

        // Inventory movement history — a read-only, clinic-wide chronological list of stock
        // movements (REQ-INV-005, reopened by DEC-INV-038). Read-only by construction: there is
        // deliberately no POST/PUT/DELETE on this resource, because movements are written only by
        // the inventory paths inside their own unit of work (BR-INV-039, BR-INV-062, AC-INV-035).
        // A literal segment, so it cannot collide with the {productId:guid} batch route below.
        app.MapGet(
            "/api/v1/inventory/movements",
            async (
                [AsParameters] InventoryHistoryRequest request,
                IQueryHandler<InventoryHistoryQuery, PagedResult<InventoryHistoryItemDto>> handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(request.ToQuery(), cancellationToken)));

        // The per-product inventory summary (REQ-INV-012) — the four inventory facts for one
        // product, so a screen outside Inventory can show them without reading Inventory's
        // tables or paging its batches. A null result means the product does not exist → 404
        // (the REQ-INV-003 precedent, AC-INV-022); a product that exists but was never
        // received is a valid summary of zero, not a 404. Registered before the {productId}
        // batch route for the same reason that one sits last: distinct literal suffixes.
        // Read-only: no write surface (BR-INV-006).
        app.MapGet(
            "/api/v1/inventory/{productId:guid}/summary",
            async (
                Guid productId,
                IQueryHandler<ProductInventorySummaryQuery, ProductInventorySummaryDto?> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new ProductInventorySummaryQuery { ProductId = productId }, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            });

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
