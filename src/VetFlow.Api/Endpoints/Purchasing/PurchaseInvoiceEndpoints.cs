using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Queries.PurchaseDetails;
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
    }
}
