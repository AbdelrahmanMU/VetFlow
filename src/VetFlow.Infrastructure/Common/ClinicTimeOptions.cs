namespace VetFlow.Infrastructure.Common;

/// <summary>
/// The one configured time zone the clinic's local date derives from (BR-INV-060) — validated
/// typed options (STD-BE-048): an absent or unresolvable time zone refuses to boot (principle 8),
/// because a system running with an unknown time zone makes the expiry safety decision
/// (DEC-INV-021) undefined. <b>Silent fallback to UTC is prohibited.</b>
///
/// One value for the whole system, not one per user or device (single-clinic deployment). No
/// Settings screen is designed in this sprint — the Settings module is undocumented and may
/// surface the value later without changing the rule.
/// </summary>
public sealed class ClinicTimeOptions
{
    public const string SectionName = "Clinic";

    /// <summary>An IANA or Windows time-zone id, e.g. <c>Africa/Cairo</c>.</summary>
    public required string TimeZone { get; init; }
}
