using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Commands.AddPurchaseLineItem;
using VetFlow.Application.Purchasing.Commands.CreatePurchaseInvoice;
using VetFlow.Application.Purchasing.Commands.RemovePurchaseLineItem;
using VetFlow.Application.Purchasing.Queries.PurchaseDetails;
using VetFlow.Application.Purchasing.Queries.PurchaseLineItems;
using VetFlow.Application.Purchasing.Queries.PurchaseList;

namespace VetFlow.Api.Endpoints.Purchasing;

public static class PurchaseInvoiceEndpoints
{
    public static void MapPurchaseInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/v1/purchase-invoices",
            async (
                [AsParameters] PurchaseListRequest request,
                IQueryHandler<PurchaseListQuery, PagedResult<PurchaseListItemDto>> handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(request.ToQuery(), cancellationToken)));

        app.MapGet(
            "/api/v1/purchase-invoices/{id:guid}",
            async (
                Guid id,
                IQueryHandler<PurchaseDetailsQuery, PurchaseDetailsDto?> handler,
                CancellationToken cancellationToken) =>
            {
                var details = await handler.HandleAsync(new PurchaseDetailsQuery { Id = id }, cancellationToken);
                return details is null ? Results.NotFound() : Results.Ok(details);
            });

        app.MapPost(
            "/api/v1/purchase-invoices",
            async (
                CreatePurchaseInvoiceRequest request,
                ICommandHandler<CreatePurchaseInvoiceCommand, CreatePurchaseInvoiceResult> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(request.ToCommand(), cancellationToken);
                return Results.Created($"/api/v1/purchase-invoices/{result.Id}", result);
            });

        app.MapGet(
            "/api/v1/purchase-invoices/{id:guid}/lines",
            async (
                Guid id,
                IQueryHandler<PurchaseLineItemsQuery, IReadOnlyList<PurchaseLineItemDto>?> handler,
                CancellationToken cancellationToken) =>
            {
                var lines = await handler.HandleAsync(new PurchaseLineItemsQuery { InvoiceId = id }, cancellationToken);
                return lines is null ? Results.NotFound() : Results.Ok(lines);
            });

        app.MapPost(
            "/api/v1/purchase-invoices/{id:guid}/lines",
            async (
                Guid id,
                AddPurchaseLineItemRequest request,
                ICommandHandler<AddPurchaseLineItemCommand, Guid?> handler,
                CancellationToken cancellationToken) =>
            {
                var lineId = await handler.HandleAsync(request.ToCommand(id), cancellationToken);
                return lineId is null
                    ? Results.NotFound()
                    : Results.Created($"/api/v1/purchase-invoices/{id}", new { lineId });
            });

        app.MapDelete(
            "/api/v1/purchase-invoices/{id:guid}/lines/{lineId:guid}",
            async (
                Guid id,
                Guid lineId,
                ICommandHandler<RemovePurchaseLineItemCommand, Guid?> handler,
                CancellationToken cancellationToken) =>
            {
                var removed = await handler.HandleAsync(
                    new RemovePurchaseLineItemCommand { InvoiceId = id, LineId = lineId },
                    cancellationToken);
                return removed is null ? Results.NotFound() : Results.NoContent();
            });
    }
}
