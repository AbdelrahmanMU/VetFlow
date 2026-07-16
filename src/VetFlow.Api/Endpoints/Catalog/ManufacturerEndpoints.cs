using VetFlow.Application.Catalog.Commands.CreateManufacturer;
using VetFlow.Application.Catalog.Commands.RenameManufacturer;
using VetFlow.Application.Catalog.Commands.SetManufacturerActive;
using VetFlow.Application.Catalog.Queries.ManufacturerList;
using VetFlow.Application.Common;

namespace VetFlow.Api.Endpoints.Catalog;

/// <summary>
/// Manufacturer management endpoints (module: Catalog — الشركات المصنعة) — the full
/// lifecycle the managed-data slice owns: list/search (REQ-CAT-047), create
/// (REQ-CAT-013), rename (REQ-CAT-013), and activate/deactivate (REQ-CAT-048). There
/// is no delete; deactivation is the official retirement (BR-CAT-051). The GET
/// repurposes the former options endpoint to the management list ({id, name,
/// isActive} — a superset), so the product-list filter and editor keep working. A
/// deliberate mirror of the category endpoints (Categories owns its own version).
/// </summary>
public static class ManufacturerEndpoints
{
    public static void MapManufacturerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/manufacturers",
            async (
                [AsParameters] ManufacturerListRequest request,
                IQueryHandler<ManufacturerListQuery, PagedResult<ManufacturerListItemDto>> handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(request.ToQuery(), cancellationToken)));

        app.MapPost(
            "/api/v1/manufacturers",
            async (
                CreateManufacturerRequest request,
                ICommandHandler<CreateManufacturerCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var id = await handler.HandleAsync(request.ToCommand(), cancellationToken);
                return Results.Created($"/api/v1/manufacturers/{id}", new { id });
            });

        app.MapPut(
            "/api/v1/manufacturers/{id:guid}",
            async (
                Guid id,
                RenameManufacturerRequest request,
                ICommandHandler<RenameManufacturerCommand, Guid?> handler,
                CancellationToken cancellationToken) =>
            {
                var updatedId = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
                return updatedId is null ? Results.NotFound() : Results.NoContent();
            });

        app.MapPost(
            "/api/v1/manufacturers/{id:guid}/activate",
            (Guid id, ICommandHandler<SetManufacturerActiveCommand, Guid?> handler, CancellationToken cancellationToken) =>
                SetActiveAsync(id, isActive: true, handler, cancellationToken));

        app.MapPost(
            "/api/v1/manufacturers/{id:guid}/deactivate",
            (Guid id, ICommandHandler<SetManufacturerActiveCommand, Guid?> handler, CancellationToken cancellationToken) =>
                SetActiveAsync(id, isActive: false, handler, cancellationToken));
    }

    private static async Task<IResult> SetActiveAsync(
        Guid id,
        bool isActive,
        ICommandHandler<SetManufacturerActiveCommand, Guid?> handler,
        CancellationToken cancellationToken)
    {
        var updatedId = await handler.HandleAsync(
            new SetManufacturerActiveCommand { Id = id, IsActive = isActive }, cancellationToken);
        return updatedId is null ? Results.NotFound() : Results.NoContent();
    }
}
