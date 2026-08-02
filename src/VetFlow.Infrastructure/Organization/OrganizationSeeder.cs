using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Identity;
using VetFlow.Domain.Identity;
using VetFlow.Domain.Organization;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Organization;

/// <summary>
/// Creates the first real clinic (ADR-0022 §10, REQ-ORG-008, REQ-IDN-009): one tenant, one
/// branch, one owner user, one membership.
///
/// <b>This is the whole onboarding procedure.</b> Adding a future customer is the same four
/// inserts in one transaction — never an infrastructure provisioning workflow (ADR-0022 §12.16).
/// That it fits in one small class is the practical proof of the tenancy decision, not a
/// coincidence of it.
///
/// The four tables written here are platform-global, so no tenant scope is required to write
/// them — which is exactly why the seeder can run before any tenant exists.
///
/// <b>Idempotent by identity, not by count.</b> It re-runs on every start-up and does nothing
/// when the rows are present, so an interrupted first boot leaves no half-built clinic and a
/// redeploy never duplicates one.
/// </summary>
public sealed class OrganizationSeeder(VetFlowDbContext dbContext, IPasswordHasher passwordHasher)
{
    // Fixed ids, following the existing seeded-reference-data convention (units use a1…,
    // natures b1…). These are seed identities, not a default scope: nothing resolves a tenant
    // from them, which is what ADR-0022 §12.6 prohibits.
    private static readonly Guid PilotTenantId = Guid.Parse("c1000000-0000-4000-8000-000000000001");
    private static readonly Guid PilotBranchId = Guid.Parse("c1000000-0000-4000-8000-000000000002");
    private static readonly Guid PilotOwnerId = Guid.Parse("c1000000-0000-4000-8000-000000000003");
    private static readonly Guid PilotMembershipId = Guid.Parse("c1000000-0000-4000-8000-000000000004");

    public const string PilotTenantName = "Happy Pets Clinic";
    public const string PilotBranchName = "Main Branch";
    public const string PilotOwnerDisplayName = "Clinic Owner";
    public const string PilotOwnerPhoneNumber = "01001127204";

    /// <summary>The seeded scope, for bootstrap and tests that need it before signing in.</summary>
    public static (Guid TenantId, Guid BranchId) PilotScope => (PilotTenantId, PilotBranchId);

    public async Task SeedAsync(string timeZone, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZone);

        if (await dbContext.Tenants.AnyAsync(tenant => tenant.Id == PilotTenantId, cancellationToken))
        {
            return;
        }

        dbContext.Tenants.Add(new Tenant(PilotTenantId, PilotTenantName, timeZone));
        dbContext.Branches.Add(new Branch(PilotBranchId, PilotTenantId, PilotBranchName));

        // The Pilot password equals the phone number by owner ruling (DEC-IDN-008). It is stored
        // hashed like any other (BR-IDN-002) — the ruling is about which password, never about
        // how it is kept. ADR-0022 §12.15 records it as a Pilot-scoped concession to revisit
        // before a second clinic exists.
        dbContext.Users.Add(new User(
            PilotOwnerId,
            PilotOwnerDisplayName,
            PilotOwnerPhoneNumber,
            passwordHasher.Hash(PilotOwnerPhoneNumber)));

        dbContext.Memberships.Add(new Membership(
            PilotMembershipId,
            PilotTenantId,
            PilotBranchId,
            PilotOwnerId,
            MembershipRole.Owner));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
