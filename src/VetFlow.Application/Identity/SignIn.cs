using VetFlow.Domain.Organization;

namespace VetFlow.Application.Identity;

/// <summary>Sign-in credentials (REQ-IDN-002). Phone number and password — nothing else.</summary>
public sealed record SignInCommand
{
    public required string PhoneNumber { get; init; }

    public required string Password { get; init; }
}

/// <summary>
/// What a successful sign-in establishes (REQ-IDN-003). The tenant and branch travel <b>inside
/// the token</b>, not as values the client may hand back — the client never names its own tenant
/// (BR-IDN-004, ADR-0022 §12.5).
/// </summary>
public sealed record SignInResult
{
    public required string AccessToken { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required string DisplayName { get; init; }
}

/// <summary>
/// The identity a token is minted for. Assembled by the sign-in handler from the user and the
/// membership, and never from anything the caller supplied.
/// </summary>
public sealed record AuthenticatedIdentity
{
    public required Guid UserId { get; init; }

    public required string DisplayName { get; init; }

    public required Guid TenantId { get; init; }

    public required Guid BranchId { get; init; }

    public required MembershipRole Role { get; init; }
}

/// <summary>
/// Mints the access token. An abstraction because ADR-0010 §3 requires the token mechanism to be
/// replaceable without touching the Application layer — today a JWT with no refresh rotation
/// (DEC-IDN-003), tomorrow whatever an external provider issues.
/// </summary>
public interface IAccessTokenIssuer
{
    (string Token, DateTimeOffset ExpiresAt) Issue(AuthenticatedIdentity identity);
}
