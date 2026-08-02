using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Common;

/// <summary>
/// Resolves a tenant's own time zone — the source of the clinic date since DEC-ORG-007 moved it
/// from deployment configuration onto the tenant (ADR-0022 §9, REQ-ORG-001). <b>BR-INV-060 is
/// unchanged</b>: the clinic date still never comes from UTC, from server time or from the user's
/// device. Only where the zone is stored moved.
///
/// <b>Cached per tenant for the lifetime of the process.</b> The clinic date is read on nearly
/// every inventory query, and an uncached lookup would add a database round trip to each one —
/// visible to the query-count assertions BR-INV-053 relies on. Nothing can change a tenant's zone
/// while the process runs: there is no settings screen and no administration surface
/// (BR-ORG-008), so the cache cannot go stale against a change the system itself can make.
///
/// <b>There is no fallback.</b> A tenant with no row, or with a zone this system cannot resolve,
/// throws — the same refusal <see cref="ClinicTimeOptions"/> performs at start-up, for the same
/// reason: a system that guesses the clinic's date has made the expiry safety decision undefined
/// (BR-ORG-007 validations, DEC-INV-021).
/// </summary>
public sealed class TenantTimeZones(IServiceScopeFactory scopeFactory)
{
    private readonly ConcurrentDictionary<Guid, TimeZoneInfo> _byTenant = new();

    public TimeZoneInfo For(Guid tenantId) => _byTenant.GetOrAdd(tenantId, Load);

    private TimeZoneInfo Load(Guid tenantId)
    {
        // Its own scope: the clock is a singleton consulted from inside other units of work, and
        // borrowing their DbContext would entangle its change tracker with a read that is not
        // theirs. Tenants carry no query filter (DEC-ORG-009), so this read needs no scope of its
        // own to succeed.
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VetFlowDbContext>();

        var timeZoneId = dbContext.Tenants
            .Where(tenant => tenant.Id == tenantId)
            .Select(tenant => tenant.TimeZone)
            .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"Tenant '{tenantId}' has no row, so its clinic time zone cannot be resolved. " +
                "The clinic date is never answered with UTC or server time (BR-INV-060, BR-ORG-007).");

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidOperationException(
                $"Tenant '{tenantId}' declares the time zone '{timeZoneId}', which this system does not know. " +
                "An unresolvable zone stops the operation rather than falling back to UTC (BR-ORG-007).",
                exception);
        }
    }
}
