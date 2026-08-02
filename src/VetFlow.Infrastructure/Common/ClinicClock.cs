using VetFlow.Application.Common;

namespace VetFlow.Infrastructure.Common;

/// <summary>
/// The clinic local date (BR-INV-059, BR-INV-060): the current instant from the injected
/// <see cref="TimeProvider"/>, converted into <b>the current tenant's</b> time zone and reduced to
/// a date. Never the UTC date, never the server's local date, never the caller's.
///
/// <b>The zone comes from the tenant, not from configuration</b> (DEC-ORG-007, ADR-0022 §9). It
/// used to be resolved once at construction from one deployment-wide setting, which is correct for
/// at most one clinic and contradicted ADR-0007's requirement that tenant-specific localization
/// stay possible. <b>BR-INV-060 itself did not change</b> — deriving the date from UTC, from
/// server time or from the user's device stays prohibited, and there is still no fallback anywhere
/// on this path (<see cref="TenantTimeZones"/> throws rather than guessing).
///
/// A singleton, like the tenant context it reads: every access resolves the current request's
/// tenant afresh, so one instance serves every tenant and none inherits another's date.
/// </summary>
public sealed class ClinicClock(
    TimeProvider timeProvider,
    ITenantContext tenantContext,
    TenantTimeZones tenantTimeZones) : IClinicClock
{
    public DateOnly Today
    {
        get
        {
            var clinicTimeZone = tenantTimeZones.For(tenantContext.TenantId);
            return DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), clinicTimeZone).DateTime);
        }
    }
}
