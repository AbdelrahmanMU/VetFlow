namespace VetFlow.Application.Common;

/// <summary>
/// The clinic's business date (BR-INV-059, BR-INV-060) — the single reference date for <b>every</b>
/// date decision in the system: batch expiry and the saleable predicate (BR-INV-050), the 30-day
/// expiring-soon horizon (BR-INV-013), the batch-viewer expiry filter (BR-INV-022), and expiry
/// monitoring (BR-INV-033/036).
///
/// <b>UTC is prohibited for business decisions</b>, as are server/OS time, the user's browser or
/// device time, and anything that varies per machine or session: whether a medicine may be sold
/// must not depend on which server answered the request. The value comes from one time zone
/// configured for the whole system (a single-clinic deployment — the DEC-INV-002 basis), and the
/// system refuses to run with an unknown time zone rather than falling back to UTC silently.
///
/// This is an abstraction only because four call sites need the same answer; the implementation is
/// a few lines over <see cref="TimeProvider"/>.
/// </summary>
public interface IClinicClock
{
    /// <summary>Today's date at the clinic — never a UTC date.</summary>
    DateOnly Today { get; }
}
