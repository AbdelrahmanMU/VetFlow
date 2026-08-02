using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using VetFlow.Application.Common;
using VetFlow.Domain.Organization;
using VetFlow.Infrastructure.Organization;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The clinic date comes from the <b>tenant's</b> time zone (AC-ORG-009, DEC-ORG-007). It used to
/// come from one deployment-wide setting resolved once at construction, which is correct for at
/// most one clinic — and expiry safety depends on it (BR-INV-059/060).
///
/// <b>BR-INV-060 is unchanged and is what these tests actually protect</b>: the date is never
/// derived from UTC, from the server, or from the caller, and an unusable zone stops the operation
/// instead of falling back.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class TenantClockTests(ApiFixture fixture)
{
    [Fact]
    public async Task Two_tenants_in_different_zones_get_different_clinic_dates_TS_ORG_010_AC_ORG_009()
    {
        // Twenty-six hours apart, so their local dates can never coincide — whatever the instant,
        // and whatever the deployment's own configured zone is. A test that used two nearby zones
        // would pass for most of the day while proving nothing.
        var farEast = await SeedTenantAsync("Far East Clinic", "Pacific/Kiritimati");
        var farWest = await SeedTenantAsync("Far West Clinic", "Etc/GMT+12");

        var eastToday = ClinicTodayFor(farEast);
        var westToday = ClinicTodayFor(farWest);

        eastToday.ShouldBeGreaterThan(westToday);
    }

    [Fact]
    public async Task An_unresolvable_tenant_zone_stops_rather_than_falling_back_to_UTC_BR_ORG_007()
    {
        var broken = await SeedTenantAsync("Unknown Zone Clinic", "Mars/Olympus");

        var failure = Should.Throw<InvalidOperationException>(() => ClinicTodayFor(broken));

        failure.Message.ShouldContain("Mars/Olympus");
    }

    [Fact]
    public void The_seeded_clinic_keeps_the_date_it_had_before_the_source_moved_TS_ORG_011_AC_ORG_010()
    {
        var configuredZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");
        var expected = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(TimeProvider.System.GetUtcNow(), configuredZone).DateTime);

        // The Pilot clinic was seeded with the configured zone (ADR-0022 §10), so moving the source
        // onto the tenant changed nothing it can observe.
        fixture.ClinicToday.ShouldBe(expected);
    }

    private DateOnly ClinicTodayFor(Guid tenantId) =>
        fixture.UsingScope(
            tenantId,
            OrganizationSeeder.PilotScope.BranchId,
            services => services.GetRequiredService<IClinicClock>().Today);

    private async Task<Guid> SeedTenantAsync(string name, string timeZone)
    {
        var tenantId = Guid.NewGuid();
        await fixture.SeedAsync(dbContext =>
        {
            dbContext.Tenants.Add(new Tenant(tenantId, name, timeZone));
            return Task.CompletedTask;
        });

        return tenantId;
    }
}
