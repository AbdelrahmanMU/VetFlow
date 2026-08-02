namespace VetFlow.Api.Security;

/// <summary>
/// Token signing and lifetime configuration (REQ-IDN-003).
///
/// The signing key is a secret and is supplied by configuration, never committed — the same
/// discipline as the database connection string. The application refuses to boot without one
/// rather than inventing a default: a predictable signing key lets anyone mint a token for any
/// tenant, which would defeat every isolation guarantee in ADR-0022 at once.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>The symmetric signing key. Minimum 32 bytes for HMAC-SHA256.</summary>
    public required string SigningKey { get; init; }

    public string Issuer { get; init; } = "vetflow";

    public string Audience { get; init; } = "vetflow";

    /// <summary>
    /// Token lifetime in hours. <b>12 by owner ruling (OQ-IDN-1, DEC-IDN-009)</b>: it covers a
    /// full clinic working day so the cashier is never interrupted mid-service, and expires
    /// between days. With no refresh token and no "remember me" (DEC-IDN-003), expiry means
    /// signing in again (BR-IDN-009).
    /// </summary>
    public int LifetimeHours { get; init; } = 12;
}
