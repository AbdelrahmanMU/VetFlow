using VetFlow.Application.Categories.Commands.CreateCategory;
using VetFlow.Application.Categories.Commands.RenameCategory;
using VetFlow.Application.Categories.Commands.SetCategoryActive;
using VetFlow.Application.Categories.Queries.CategoryList;
using VetFlow.Application.Common;

namespace VetFlow.Api.Endpoints.Categories;

/// <summary>
/// Category management endpoints (module: التصنيفات) — the full lifecycle the
/// managed-data slice owns: list/search (REQ-CTG-001), create (REQ-CTG-002),
/// rename (REQ-CTG-003), and activate/deactivate (REQ-CTG-004). There is no delete;
/// deactivation is the official retirement (BR-CTG-006).
/// </summary>
public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/categories",
            async (
                [AsParameters] CategoryListRequest request,
                IQueryHandler<CategoryListQuery, PagedResult<CategoryListItemDto>> handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(request.ToQuery(), cancellationToken)));

        app.MapPost(
            "/api/v1/categories",
            async (
                CreateCategoryRequest request,
                ICommandHandler<CreateCategoryCommand, Guid> handler,
                CancellationToken cancellationToken) =>
            {
                var id = await handler.HandleAsync(request.ToCommand(), cancellationToken);
                return Results.Created($"/api/v1/categories/{id}", new { id });
            });

        app.MapPut(
            "/api/v1/categories/{id:guid}",
            async (
                Guid id,
                RenameCategoryRequest request,
                ICommandHandler<RenameCategoryCommand, Guid?> handler,
                CancellationToken cancellationToken) =>
            {
                var updatedId = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
                return updatedId is null ? Results.NotFound() : Results.NoContent();
            });

        app.MapPost(
            "/api/v1/categories/{id:guid}/activate",
            (Guid id, ICommandHandler<SetCategoryActiveCommand, Guid?> handler, CancellationToken cancellationToken) =>
                SetActiveAsync(id, isActive: true, handler, cancellationToken));

        app.MapPost(
            "/api/v1/categories/{id:guid}/deactivate",
            (Guid id, ICommandHandler<SetCategoryActiveCommand, Guid?> handler, CancellationToken cancellationToken) =>
                SetActiveAsync(id, isActive: false, handler, cancellationToken));
    }

    private static async Task<IResult> SetActiveAsync(
        Guid id,
        bool isActive,
        ICommandHandler<SetCategoryActiveCommand, Guid?> handler,
        CancellationToken cancellationToken)
    {
        var updatedId = await handler.HandleAsync(
            new SetCategoryActiveCommand { Id = id, IsActive = isActive }, cancellationToken);
        return updatedId is null ? Results.NotFound() : Results.NoContent();
    }
}
