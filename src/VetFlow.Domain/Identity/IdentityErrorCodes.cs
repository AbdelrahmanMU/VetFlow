namespace VetFlow.Domain.Identity;

/// <summary>Identity error codes (ADR-0018, ADR-0015 §4).</summary>
public static class IdentityErrorCodes
{
    /// <summary>
    /// Sign-in failed. <b>One code for every cause</b> — unknown phone number, wrong password, and
    /// a user with no membership all produce this and nothing else (BR-IDN-003, AC-IDN-003).
    /// Distinguishing them would tell an attacker which phone numbers are registered.
    /// </summary>
    public const string SignInFailed = "VTF-IDN-001";

    /// <summary>
    /// The request carried no valid authentication. Business endpoints never serve anonymous
    /// callers (BR-IDN-005, REQ-IDN-006).
    /// </summary>
    public const string NotAuthenticated = "VTF-IDN-002";
}
