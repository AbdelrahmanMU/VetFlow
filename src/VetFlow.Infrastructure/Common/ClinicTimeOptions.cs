namespace VetFlow.Infrastructure.Common;

/// <summary>
/// The time zone a <b>newly seeded clinic starts with</b> (ADR-0022 §10). Since DEC-ORG-007 the
/// running system reads the zone from the tenant row rather than from here — a singleton clock
/// over one deployment-wide setting is correct for at most one clinic. This value is what the very
/// first clinic is created with, which is why the Pilot's dates are byte-identical to what they
/// were before the move.
///
/// It stays validated typed options (STD-BE-048) and still <b>refuses to boot</b> when absent or
/// unresolvable (principle 8): the seeded clinic would otherwise be created carrying a zone nobody
/// checked, and the expiry safety decision (DEC-INV-021) would be undefined from its first day.
/// <b>Silent fallback to UTC is prohibited</b> here and at every later reader (BR-INV-060,
/// BR-ORG-007).
/// </summary>
public sealed class ClinicTimeOptions
{
    public const string SectionName = "Clinic";

    /// <summary>An IANA or Windows time-zone id, e.g. <c>Africa/Cairo</c>.</summary>
    public required string TimeZone { get; init; }
}
