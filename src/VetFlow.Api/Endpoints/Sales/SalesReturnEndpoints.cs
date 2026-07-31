using VetFlow.Application.Common;
using VetFlow.Application.Sales.Commands.AddSalesReturnLine;
using VetFlow.Application.Sales.Commands.CommitSalesReturn;
using VetFlow.Application.Sales.Commands.CreateSalesReturn;
using VetFlow.Application.Sales.Commands.RemoveSalesReturnLine;
using VetFlow.Application.Sales.Queries.SalesReturnableLines;

namespace VetFlow.Api.Endpoints.Sales;

/// <summary>
/// Sales-return endpoints (REQ-SAL-004, DEC-SAL-010).
///
/// <para>There is deliberately <b>no DELETE for a return</b> and <b>no cancel endpoint</b>: a
/// committed return has no reversal path (DEC-INV-037), and exposing one would contradict the rule
/// at the surface even if the handler refused it (AC-SAL-020).</para>
/// </summary>
public static class SalesReturnEndpoints
{
    public static void MapSalesReturnEndpoints(this IEndpointRouteBuilder app)
    {
        // The screen's read: the invoice's lines with what remains returnable (BR-SAL-016). 404 also
        // covers "invoice exists but is not Committed" (BR-SAL-015) — a screen that cannot legally
        // produce a return does not render a table the command would reject.
        app.MapGet(
            "/api/v1/sales-invoices/{id:guid}/returnable-lines",
            async (
                Guid id,
                IQueryHandler<SalesReturnableLinesQuery, IReadOnlyList<SalesReturnableLineDto>?> handler,
                CancellationToken cancellationToken) =>
            {
                var lines = await handler.HandleAsync(new SalesReturnableLinesQuery(id), cancellationToken);
                return lines is null ? Results.NotFound() : Results.Ok(lines);
            });

        app.MapPost(
            "/api/v1/sales-returns",
            async (
                CreateSalesReturnRequest request,
                ICommandHandler<CreateSalesReturnCommand, CreateSalesReturnResult?> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
                return result is null
                    ? Results.NotFound()
                    : Results.Created($"/api/v1/sales-returns/{result.Id}", result);
            });

        app.MapPost(
            "/api/v1/sales-returns/{id:guid}/lines",
            async (
                Guid id,
                AddSalesReturnLineRequest request,
                ICommandHandler<AddSalesReturnLineCommand, Guid?> handler,
                CancellationToken cancellationToken) =>
            {
                var lineId = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
                return lineId is null
                    ? Results.NotFound()
                    : Results.Created($"/api/v1/sales-returns/{id}/lines/{lineId}", new { id = lineId });
            });

        app.MapDelete(
            "/api/v1/sales-returns/{id:guid}/lines/{lineId:guid}",
            async (
                Guid id,
                Guid lineId,
                ICommandHandler<RemoveSalesReturnLineCommand, bool> handler,
                CancellationToken cancellationToken) =>
            {
                var removed = await handler.HandleAsync(
                    new RemoveSalesReturnLineCommand { SalesReturnId = id, SalesReturnLineId = lineId },
                    cancellationToken);
                return removed ? Results.NoContent() : Results.NotFound();
            });

        app.MapPost(
            "/api/v1/sales-returns/{id:guid}/commit",
            async (
                Guid id,
                ICommandHandler<CommitSalesReturnCommand, bool> handler,
                CancellationToken cancellationToken) =>
            {
                var committed = await handler.HandleAsync(
                    new CommitSalesReturnCommand { SalesReturnId = id },
                    cancellationToken);
                return committed ? Results.NoContent() : Results.NotFound();
            });
    }
}
