namespace VetFlow.Domain.Organization;

/// <summary>
/// A physical site belonging to one tenant (REQ-ORG-002). It is the scope of business
/// documents, their numbering, and inventory (BR-ORG-002).
///
/// <b>A future warehouse is modelled as a Branch, not as a level below it</b> — ADR-0022
/// §11.1 and §12.9. That constraint is what keeps the owner's two-level hierarchy additive:
/// introducing a stock-location level would reinstate a primary-key change on a live
/// <c>product_on_hands</c>, the most expensive migration in the system.
///
/// There is no branch UI, no branch switching and no transfers in this phase (owner ruling,
/// 2026-08-02); the branch exists as the organizational foundation only.
/// </summary>
public sealed class Branch
{
    private Branch()
    {
        // EF Core materialization only.
        Name = string.Empty;
    }

    public Branch(Guid id, Guid tenantId, string name)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(tenantId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        TenantId = tenantId;
        Name = name.Trim();
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public string Name { get; private set; }
}
