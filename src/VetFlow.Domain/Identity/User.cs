namespace VetFlow.Domain.Identity;

/// <summary>
/// A person who signs in, and to whom every write is attributed (REQ-IDN-001, REQ-IDN-008).
/// This supersedes the optional free-text <c>ActorName</c> as the attribution mechanism
/// (BR-INV-066, amended 2026-08-02); the historical values stay readable and are neither
/// deleted nor rewritten.
///
/// <b>The phone number is unique across the entire system, not per tenant</b> (BR-IDN-001,
/// owner ruling OQ-IDN-4, ADR-0022 §12.14). Tenant-scoped uniqueness would require knowing the
/// tenant <i>before</i> knowing the user, which either re-introduces a client-supplied tenant
/// identifier at the least authenticated moment in the system or forces one person to hold
/// several accounts.
///
/// The password is only ever held hashed (BR-IDN-002). The hash algorithm is an Infrastructure
/// concern behind the ADR-0010 authentication abstraction; the domain stores an opaque string
/// and never inspects it.
/// </summary>
public sealed class User
{
    private User()
    {
        // EF Core materialization only.
        DisplayName = string.Empty;
        PhoneNumber = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(Guid id, string displayName, string phoneNumber, string passwordHash)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        Id = id;
        DisplayName = displayName.Trim();
        PhoneNumber = phoneNumber.Trim();
        PasswordHash = passwordHash;
    }

    public Guid Id { get; }

    public string DisplayName { get; private set; }

    /// <summary>The sign-in identifier (REQ-IDN-002). Unique platform-wide (BR-IDN-001).</summary>
    public string PhoneNumber { get; private set; }

    /// <summary>
    /// The hashed password — <b>never the plain text</b> (BR-IDN-002). Never logged, never
    /// returned in any response (AC-IDN-004).
    /// </summary>
    public string PasswordHash { get; private set; }

    /// <summary>
    /// Replaces the stored hash. The only mutation the Pilot needs; there is no self-service
    /// reset path by ruling (BR-IDN-010), so this exists for operational correction only.
    /// </summary>
    public void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        PasswordHash = passwordHash;
    }
}
