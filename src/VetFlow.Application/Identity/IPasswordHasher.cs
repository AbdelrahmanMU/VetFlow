namespace VetFlow.Application.Identity;

/// <summary>
/// Hashing and verification of passwords, behind the abstraction ADR-0010 §2/§3 makes mandatory:
/// the Application layer must never couple to an identity provider, so that replacing the
/// provider is an Infrastructure swap rather than a redesign.
///
/// Implementations must never expose, log or return the plain password (BR-IDN-002).
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plain password for storage. The result is opaque to every caller.</summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a plain password against a stored hash. Implementations should compare in a way
    /// that does not leak timing information, which the framework hasher already does.
    /// </summary>
    bool Verify(string hash, string password);
}
