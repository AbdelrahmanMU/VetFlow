using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VetFlow.Application.Identity;

namespace VetFlow.Api.Security;

/// <summary>
/// Mints the single access token a successful sign-in produces (REQ-IDN-003, DEC-IDN-003).
/// Confined to the Api behind <see cref="IAccessTokenIssuer"/>, so the token mechanism can be
/// replaced without the Application layer noticing — ADR-0010 §3.
///
/// The tenant and branch are claims <b>because</b> they must not be anything else: putting them
/// in the token is what lets every later request resolve its scope from something the client
/// cannot forge (BR-IDN-004, ADR-0022 §12.5).
/// </summary>
public sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider)
    : IAccessTokenIssuer
{
    public (string Token, DateTimeOffset ExpiresAt) Issue(AuthenticatedIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var settings = options.Value;
        var expiresAt = timeProvider.GetUtcNow().AddHours(settings.LifetimeHours);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims:
            [
                new Claim(VetFlowClaims.UserId, identity.UserId.ToString()),
                new Claim(VetFlowClaims.DisplayName, identity.DisplayName),
                new Claim(VetFlowClaims.TenantId, identity.TenantId.ToString()),
                new Claim(VetFlowClaims.BranchId, identity.BranchId.ToString()),
                new Claim(VetFlowClaims.Role, identity.Role.ToString()),
                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString(),
                    ClaimValueTypes.String,
                    settings.Issuer),
            ],
            notBefore: timeProvider.GetUtcNow().UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    /// <summary>Formats the lifetime for the runbook and for diagnostics.</summary>
    public static string DescribeLifetime(JwtOptions settings) =>
        string.Create(CultureInfo.InvariantCulture, $"{settings.LifetimeHours}h");
}
