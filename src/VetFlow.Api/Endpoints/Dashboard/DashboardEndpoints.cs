using VetFlow.Application.Common;
using VetFlow.Application.Dashboard.Queries.OperationalDashboard;

namespace VetFlow.Api.Endpoints.Dashboard;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        // The operational dashboard (REQ-DSH-010) — the whole board in one request, never one
        // request per card (BR-DSH-019).
        //
        // No query parameters, deliberately (BR-DSH-020): the board is not filtered, sorted,
        // paged or personalised. Its scope comes from the token (tenant, branch) and the
        // clinic local date — nothing the caller supplies.
        //
        // Always 200 when the caller is authenticated, even if a section could not be read:
        // each section carries its own status, and a failed one is omitted from the payload
        // rather than reported as zero (BR-DSH-014, DEC-DSH-002). Authentication is the
        // fallback policy's job — no AllowAnonymous here, so a missing token is a 401 through
        // the existing identity path (REQ-IDN-006) and not an empty board.
        //
        // Read-only: there is deliberately no POST/PUT/DELETE on this resource. The dashboard
        // carries no business action at all (BR-DSH-020) — every element on it navigates.
        app.MapGet(
            "/api/v1/dashboard",
            async (
                IQueryHandler<OperationalDashboardQuery, OperationalDashboardDto> handler,
                CancellationToken cancellationToken) =>
                Results.Ok(await handler.HandleAsync(new OperationalDashboardQuery(), cancellationToken)));
    }
}
