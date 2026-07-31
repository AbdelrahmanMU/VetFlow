using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Commands.AddPurchaseReturnLine;
using VetFlow.Application.Purchasing.Commands.CommitPurchaseReturn;
using VetFlow.Application.Purchasing.Commands.CreatePurchaseReturn;
using VetFlow.Application.Purchasing.Commands.RemovePurchaseReturnLine;
using VetFlow.Application.Purchasing.Queries.PurchaseReturnableLines;

namespace VetFlow.Api.Endpoints.Purchasing;

/// <summary>
/// Purchase-return endpoints (REQ-PUR-006, DEC-PUR-010).
///
/// <para>There is deliberately <b>no DELETE for a return</b> and <b>no cancel endpoint</b>: a
/// committed return has no reversal path (DEC-INV-037), and exposing one would contradict the rule
/// at the surface even if the handler refused it.</para>
/// </summary>
public static class PurchaseReturnEndpoints
{
    public static void MapPurchaseReturnEndpoints(this IEndpointRouteBuilder app)
    {
        // The screen's read: the invoice's lines with what remains returnable (BR-PUR-016).
        // 404 also covers "invoice exists but is not Received" (BR-PUR-015) — a screen that cannot
        // legally produce a return does not render a table the command would reject.
        app.MapGet(
            "/api/v1/purchase-invoices/{id:guid}/returnable-lines",
            async (
                Guid id,
                IQueryHandler<PurchaseReturnableLinesQuery, IReadOnlyList<PurchaseReturnableLineDto>?> handler,
                CancellationToken cancellationToken) =>
            {
                var lines = await handler.HandleAsync(new PurchaseReturnableLinesQuery(id), cancellationToken);
                return lines is null ? Results.NotFound() : Results.Ok(lines);
            });

        app.MapPost(
            "/api/v1/purchase-returns",
            async (
                CreatePurchaseReturnRequest request,
                ICommandHandler<CreatePurchaseReturnCommand, CreatePurchaseReturnResult?> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
                return result is null
                    ? Results.NotFound()
                    : Results.Created($"/api/v1/purchase-returns/{result.Id}", result);
            });

        app.MapPost(
            "/api/v1/purchase-returns/{id:guid}/lines",
            async (
                Guid id,
                AddPurchaseReturnLineRequest request,
                ICommandHandler<AddPurchaseReturnLineCommand, Guid?> handler,
                CancellationToken cancellationToken) =>
            {
                var lineId = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
                return lineId is null
                    ? Results.NotFound()
                    : Results.Created($"/api/v1/purchase-returns/{id}/lines/{lineId}", new { id = lineId });
            });

        app.MapDelete(
            "/api/v1/purchase-returns/{id:guid}/lines/{lineId:guid}",
            async (
                Guid id,
                Guid lineId,
                ICommandHandler<RemovePurchaseReturnLineCommand, bool> handler,
                CancellationToken cancellationToken) =>
            {
                var removed = await handler.HandleAsync(
                    new RemovePurchaseReturnLineCommand { PurchaseReturnId = id, PurchaseReturnLineId = lineId },
                    cancellationToken);
                return removed ? Results.NoContent() : Results.NotFound();
            });

        app.MapPost(
            "/api/v1/purchase-returns/{id:guid}/commit",
            async (
                Guid id,
                ICommandHandler<CommitPurchaseReturnCommand, bool> handler,
                CancellationToken cancellationToken) =>
            {
                var committed = await handler.HandleAsync(
                    new CommitPurchaseReturnCommand { PurchaseReturnId = id },
                    cancellationToken);
                return committed ? Results.NoContent() : Results.NotFound();
            });
    }
}
