using VetFlow.Application.Common;
using VetFlow.Infrastructure.Organization;
using VetFlow.Infrastructure.Tenancy;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The organizational scope for tests that build their own <c>VetFlowDbContext</c> — a second
/// connection proving per-batch concurrency detection (BR-INV-056), or one with a command
/// interceptor counting the queries an operation issues (BR-INV-053).
///
/// It prefers <see cref="SystemTenantScope"/>, which the fixture establishes around its helpers,
/// and otherwise falls back to the seeded clinic — the same tenant and branch the fixture signs
/// into. Either way a hand-built context sees exactly what the API pipeline sees, which is the
/// point: a different scope here would let these tests pass against rows the real request path
/// could never reach.
///
/// The fallback is a <b>test-only</b> convenience and is not the "default tenant" ADR-0022 §12.6
/// prohibits — production code has no such path, and an architecture test pins that an unresolved
/// scope throws there.
/// </summary>
public sealed class TestTenantContext : ITenantContext
{
    public Guid TenantId => SystemTenantScope.CurrentScope?.TenantId ?? OrganizationSeeder.PilotScope.TenantId;

    public Guid BranchId => SystemTenantScope.CurrentScope?.BranchId ?? OrganizationSeeder.PilotScope.BranchId;

    public bool IsResolved => true;
}
