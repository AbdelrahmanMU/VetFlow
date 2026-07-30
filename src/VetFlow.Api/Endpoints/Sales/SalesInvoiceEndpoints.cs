using VetFlow.Application.Common;
using VetFlow.Application.Sales.Commands.AddSalesLineItem;
using VetFlow.Application.Sales.Commands.CommitSalesInvoice;
using VetFlow.Application.Sales.Commands.CreateSalesInvoice;
using VetFlow.Application.Sales.Commands.RemoveSalesLineItem;
using VetFlow.Application.Sales.Queries.SalesDetails;
using VetFlow.Application.Sales.Queries.SalesLineItems;

namespace VetFlow.Api.Endpoints.Sales;

/// <summary>
/// The Sales HTTP surface (REQ-SAL-001/002/003), mirroring the purchase-invoice endpoints. There is
/// deliberately <b>no list endpoint</b>: a sales list is not among the five slices and was not
/// invented (DEC-SAL-005 — still open). Commit is a POST action on the invoice, exactly like
/// receive.
/// </summary>
public static class SalesInvoiceEndpoints
{
    public static void MapSalesInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/sales-invoices/{id:guid}",
            async (
                Guid id,
                IQueryHandler<SalesDetailsQuery, SalesDetailsDto?> handler,
                CancellationToken cancellationToken) =>
            {
                var details = await handler.HandleAsync(new SalesDetailsQuery { Id = id }, cancellationToken);
                return details is null ? Results.NotFound() : Results.Ok(details);
            });

        app.MapPost(
            "/api/v1/sales-invoices",
            async (
                CreateSalesInvoiceRequest request,
                ICommandHandler<CreateSalesInvoiceCommand, CreateSalesInvoiceResult> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
                return Results.Created($"/api/v1/sales-invoices/{result.Id}", result);
            });

        app.MapGet(
            "/api/v1/sales-invoices/{id:guid}/lines",
            async (
                Guid id,
                IQueryHandler<SalesLineItemsQuery, IReadOnlyList<SalesLineItemDto>?> handler,
                CancellationToken cancellationToken) =>
            {
                var lines = await handler.HandleAsync(new SalesLineItemsQuery { InvoiceId = id }, cancellationToken);
                return lines is null ? Results.NotFound() : Results.Ok(lines);
            });

        app.MapPost(
            "/api/v1/sales-invoices/{id:guid}/lines",
            async (
                Guid id,
                AddSalesLineItemRequest request,
                ICommandHandler<AddSalesLineItemCommand, Guid?> handler,
                CancellationToken cancellationToken) =>
            {
                var lineId = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
                return lineId is null
                    ? Results.NotFound()
                    : Results.Created($"/api/v1/sales-invoices/{id}", new { lineId });
            });

        app.MapDelete(
            "/api/v1/sales-invoices/{id:guid}/lines/{lineId:guid}",
            async (
                Guid id,
                Guid lineId,
                ICommandHandler<RemoveSalesLineItemCommand, Guid?> handler,
                CancellationToken cancellationToken) =>
            {
                var removed = await handler.HandleAsync(
                    new RemoveSalesLineItemCommand { InvoiceId = id, LineId = lineId },
                    cancellationToken);
                return removed is null ? Results.NotFound() : Results.NoContent();
            });

        // The single stock-consuming action in the system (REQ-SAL-003, BR-SAL-009). It carries no
        // body: everything it needs is already on the invoice, and batch choice is Inventory's
        // (BR-SAL-013).
        app.MapPost(
            "/api/v1/sales-invoices/{id:guid}/commit",
            async (
                Guid id,
                ICommandHandler<CommitSalesInvoiceCommand, Guid?> handler,
                CancellationToken cancellationToken) =>
            {
                var committed = await handler.HandleAsync(
                    new CommitSalesInvoiceCommand { InvoiceId = id },
                    cancellationToken);
                return committed is null ? Results.NotFound() : Results.NoContent();
            });
    }
}
