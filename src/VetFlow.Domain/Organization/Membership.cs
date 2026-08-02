namespace VetFlow.Domain.Organization;

/// <summary>
/// The link between a user and a tenant, carrying that user's role (REQ-ORG-003).
///
/// <b>Access is derived from the membership, never from a field on the user</b> (BR-ORG-005,
/// ADR-0022 §12.10). A single branch or tenant foreign key on the user would force one person
/// working at two clinics to hold two accounts, which splits credentials and attribution
/// irreversibly.
///
/// <see cref="UserId"/> is a plain <see cref="Guid"/> and never an Identity type: Organization
/// and Identity must not depend on each other (STD-BE-005). This follows the precedent set when
/// Inventory began storing a sale-line id without referencing Sales (REQ-INV-008).
/// </summary>
public sealed class Membership
{
    private Membership()
    {
        // EF Core materialization only.
    }

    public Membership(Guid id, Guid tenantId, Guid branchId, Guid userId, MembershipRole role)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(branchId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);

        Id = id;
        TenantId = tenantId;
        BranchId = branchId;
        UserId = userId;
        Role = role;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    /// <summary>
    /// The branch this membership works at — the user's branch for every operation, since there
    /// is no branch selection and no switching in this phase (BR-ORG-003, workflow.md).
    /// </summary>
    public Guid BranchId { get; }

    public Guid UserId { get; }

    public MembershipRole Role { get; private set; }
}
