using VetFlow.Domain.Organization;

namespace VetFlow.Application.Common;

/// <summary>
/// Who is performing the current operation (REQ-IDN-005). Every write is attributed to this
/// user (REQ-IDN-008), superseding the optional free-text <c>ActorName</c> (BR-INV-066, amended
/// 2026-08-02).
///
/// Provider-neutral by mandate: the Application layer sees a user id, a display name and a role —
/// never an authentication framework's types (ADR-0010 §2/§3). Replacing the identity provider is
/// an Infrastructure swap, and this interface is the seam that makes it one.
///
/// Like <see cref="ITenantContext"/>, it is populated from authenticated claims only
/// (BR-IDN-004).
/// </summary>
public interface ICurrentUser
{
    /// <summary>The authenticated user's id. Throws when no user is authenticated.</summary>
    Guid UserId { get; }

    /// <summary>The authenticated user's display name, for attribution and for the shell.</summary>
    string DisplayName { get; }

    /// <summary>
    /// The role carried by this user's membership in the current tenant (BR-ORG-005/006,
    /// BR-IDN-006) — read from the membership, never from a field on the user.
    /// </summary>
    MembershipRole Role { get; }

    /// <summary>Whether a user is authenticated on this request.</summary>
    bool IsAuthenticated { get; }
}
