using System.Globalization;
using System.Security.Claims;
using VetFlow.Application.Common;
using VetFlow.Domain.Organization;
using VetFlow.Infrastructure.Tenancy;

namespace VetFlow.Api.Security;

/// <summary>
/// Resolves the current tenant, branch and user from the authenticated principal's claims
/// (ADR-0022 §3, BR-IDN-004) — the single implementation of both organizational abstractions.
///
/// <b>Registered as a singleton, deliberately.</b> EF caches the model, and the global query
/// filters built in <c>TenantScope.ApplyReadFilters</c> capture this instance once for the
/// lifetime of the process. A scoped registration would pin the first request's tenant into the
/// cached model and then serve that tenant's rows to every later request — a total cross-tenant
/// leak that no single-request test would reveal. Being a singleton is therefore not an
/// optimisation; it is what makes the filter correct. Every property reads
/// <see cref="IHttpContextAccessor"/> afresh, so the value is always the current request's.
///
/// <see cref="SystemTenantScope"/> takes precedence when set, because seeding and integration
/// tests legitimately run outside a request — the code that creates the very first tenant cannot
/// resolve one from a token that does not exist yet.
///
/// An unresolved scope <b>throws</b> rather than returning a default. There is no fallback tenant
/// (ADR-0022 §12.6): a fallback becomes load-bearing, and the first genuine second tenant then
/// reads or writes another tenant's data silently.
/// </summary>
public sealed class RequestTenantContext(IHttpContextAccessor httpContextAccessor)
    : ITenantContext, ICurrentUser
{
    public Guid TenantId => ReadGuid(VetFlowClaims.TenantId, SystemTenantScope.CurrentScope?.TenantId);

    public Guid BranchId => ReadGuid(VetFlowClaims.BranchId, SystemTenantScope.CurrentScope?.BranchId);

    public bool IsResolved => SystemTenantScope.CurrentScope is not null || TryReadClaim(VetFlowClaims.TenantId) is not null;

    public Guid UserId => ReadGuid(VetFlowClaims.UserId, ambient: null);

    public string DisplayName =>
        TryReadClaim(VetFlowClaims.DisplayName)
        ?? throw new InvalidOperationException("No authenticated user on this request.");

    public MembershipRole Role =>
        Enum.TryParse<MembershipRole>(TryReadClaim(VetFlowClaims.Role), ignoreCase: false, out var role)
            ? role
            : throw new InvalidOperationException("No authenticated user on this request.");

    public bool IsAuthenticated => TryReadClaim(VetFlowClaims.UserId) is not null;

    private Guid ReadGuid(string claimType, Guid? ambient)
    {
        // The explicit scope wins: it is only ever established by seeding, bootstrap or tests,
        // all of which run with no principal at all.
        if (ambient is { } value)
        {
            return value;
        }

        var claim = TryReadClaim(claimType);
        if (claim is null)
        {
            throw new InvalidOperationException(
                $"No organizational scope on this request: the '{claimType}' claim is absent. " +
                "The scope comes from authenticated claims only, and never falls back to a default (ADR-0022 §12.5/§12.6).");
        }

        return Guid.Parse(claim, CultureInfo.InvariantCulture);
    }

    private string? TryReadClaim(string claimType) =>
        httpContextAccessor.HttpContext?.User is { Identity.IsAuthenticated: true } principal
            ? principal.FindFirstValue(claimType)
            : null;
}
