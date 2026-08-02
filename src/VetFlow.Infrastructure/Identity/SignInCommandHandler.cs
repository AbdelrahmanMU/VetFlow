using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Identity;
using VetFlow.Domain.Common;
using VetFlow.Domain.Identity;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Identity;

/// <summary>
/// Signs a user in (REQ-IDN-002) and establishes the organizational scope every later request
/// runs under (workflow.md).
///
/// <b>Every failure returns the same rejection.</b> Unknown phone number, wrong password, and a
/// valid user with no membership are indistinguishable to the caller — same code, same status,
/// same message (BR-IDN-003, BR-IDN-007, pinned by TS-IDN-003/004/005). Telling the two apart
/// would let anyone enumerate which phone numbers are registered.
///
/// The password is verified <b>even when no user was found</b>, against a throwaway hash, so that
/// an unknown number and a wrong password take comparable time. Returning early on "no such user"
/// leaks the same fact through timing that the unified message exists to hide.
///
/// This handler reads <see cref="VetFlowDbContext.Users"/> and
/// <see cref="VetFlowDbContext.Memberships"/>, which are deliberately unfiltered — see
/// <c>OrganizationConfigurations</c> for why the rows that define tenancy cannot themselves be
/// tenant-filtered.
/// </summary>
public sealed class SignInCommandHandler(
    VetFlowDbContext dbContext,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer tokenIssuer)
{
    /// <summary>
    /// A well-formed hash of a value no one knows, used to spend the same work verifying a
    /// password for a phone number that does not exist.
    /// </summary>
    private static readonly string DecoyHash = new PasswordHasherAdapter().Hash(Guid.NewGuid().ToString());

    public async Task<SignInResult> HandleAsync(SignInCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var phoneNumber = command.PhoneNumber?.Trim() ?? string.Empty;

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.PhoneNumber == phoneNumber, cancellationToken);

        var passwordMatches = passwordHasher.Verify(user?.PasswordHash ?? DecoyHash, command.Password ?? string.Empty);

        if (user is null || !passwordMatches)
        {
            throw Rejected();
        }

        var membership = await dbContext.Memberships
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserId == user.Id, cancellationToken);

        // A user with no membership has no tenant, so there is no scope to build a session on
        // (BR-IDN-007). It is a refusal, not a system error.
        if (membership is null)
        {
            throw Rejected();
        }

        var (token, expiresAt) = tokenIssuer.Issue(new AuthenticatedIdentity
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            TenantId = membership.TenantId,
            BranchId = membership.BranchId,
            Role = membership.Role,
        });

        return new SignInResult
        {
            AccessToken = token,
            ExpiresAt = expiresAt,
            DisplayName = user.DisplayName,
        };
    }

    private static BusinessRuleException Rejected() => new(IdentityErrorCodes.SignInFailed);
}
