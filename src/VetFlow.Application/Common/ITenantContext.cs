namespace VetFlow.Application.Common;

/// <summary>
/// The organizational scope of the current operation: which tenant it belongs to, and which
/// branch (ADR-0022 §3, REQ-ORG-006).
///
/// <b>It is resolved from the authenticated principal's claims and from nowhere else</b> — never
/// from configuration, a request header, a route parameter, a query string or a request body
/// (BR-IDN-004, ADR-0022 §12.5). A client-controlled tenant identifier combined with one missing
/// filter is a complete cross-tenant read, which is the highest-severity failure mode in the
/// system; closing that path is the entire reason this abstraction exists rather than a
/// parameter.
///
/// <b>No layer may hold a fixed tenant identifier</b> — no constant, no default, no "pilot
/// tenant" fallback (ADR-0022 §12.6). A fallback becomes load-bearing, and the first real second
/// tenant then writes silently into the first one's data.
///
/// The Application layer depends on this interface, never on an authentication provider — the
/// independence ADR-0010 makes mandatory.
/// </summary>
public interface ITenantContext
{
    /// <summary>
    /// The tenant every row read or written by this operation belongs to (BR-ORG-001).
    /// Throws if no scope has been established: an unscoped write is never answered with a guess.
    /// </summary>
    Guid TenantId { get; }

    /// <summary>
    /// The branch this operation's documents, batches and movements belong to (BR-ORG-002).
    /// Derived from the user's membership; never selected by the user, because there is no branch
    /// switching in this phase (BR-ORG-003).
    /// </summary>
    Guid BranchId { get; }

    /// <summary>
    /// Whether a scope has been established. False before authentication has run — used by the
    /// persistence layer to distinguish "no tenant yet" from "wrong tenant", never to substitute
    /// a default.
    /// </summary>
    bool IsResolved { get; }
}
