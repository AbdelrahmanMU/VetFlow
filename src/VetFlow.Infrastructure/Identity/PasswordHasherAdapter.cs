using Microsoft.AspNetCore.Identity;
using VetFlow.Application.Identity;
using VetFlow.Domain.Identity;

namespace VetFlow.Infrastructure.Identity;

/// <summary>
/// The ASP.NET Core Identity password hasher, confined to Infrastructure behind
/// <see cref="IPasswordHasher"/> — exactly the arrangement ADR-0010 permits (§1: Identity is
/// allowed in the MVP) and requires (§2: nothing outside Infrastructure may know about it).
/// Recorded as DEC-IDN-004.
///
/// The framework type is used rather than a hand-rolled PBKDF2 because password hashing is a
/// primitive whose parameters need to age well; Microsoft versions its format and can migrate it,
/// and this adapter is the one place a future change would land.
///
/// <see cref="PasswordVerificationResult.SuccessRehashNeeded"/> is treated as success: the
/// password is correct and the stored hash merely uses older parameters. Rehashing on sign-in
/// would need a write path on the read side of authentication, and there is no password-change
/// capability in this phase (BR-IDN-010) — so it is deliberately not attempted here rather than
/// half-built.
/// </summary>
public sealed class PasswordHasherAdapter : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        // The framework hasher takes the user only to allow per-user salting strategies it does
        // not actually use for the default format; passing a throwaway instance keeps this
        // adapter free of any dependency on a real user being loaded.
        return _hasher.HashPassword(PlaceholderUser, password);
    }

    public bool Verify(string hash, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        var result = _hasher.VerifyHashedPassword(PlaceholderUser, hash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    private static User PlaceholderUser { get; } =
        new(Guid.Parse("00000000-0000-4000-8000-00000000ffff"), "hasher", "hasher", "hasher");
}
