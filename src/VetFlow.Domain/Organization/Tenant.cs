namespace VetFlow.Domain.Organization;

/// <summary>
/// The commercial customer: one veterinary business (REQ-ORG-001). It is the boundary of
/// data ownership, security and subscription — every business row belongs to exactly one
/// (BR-ORG-001), and nothing but the shared reference vocabulary is exempt (ADR-0022 §12.1).
///
/// The tenant owns the clinic time zone (BR-ORG-007, DEC-ORG-007). It moved here from
/// deployment configuration because a singleton clock is correct for at most one tenant;
/// <b>BR-INV-060 itself is unchanged</b> — deriving the clinic date from UTC, from server
/// time or from the user's device stays prohibited, and only the source moved.
/// </summary>
public sealed class Tenant
{
    private Tenant()
    {
        // EF Core materialization only.
        Name = string.Empty;
        TimeZone = string.Empty;
    }

    public Tenant(Guid id, string name, string timeZone)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(timeZone);

        Id = id;
        Name = name.Trim();
        TimeZone = timeZone.Trim();
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    /// <summary>
    /// The IANA/Windows time-zone id the clinic's business date is derived from (BR-ORG-007).
    /// An unresolvable value is refused at the boundary that reads it — never answered with UTC.
    /// </summary>
    public string TimeZone { get; private set; }
}
