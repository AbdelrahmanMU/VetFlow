namespace VetFlow.Domain.Catalog;

/// <summary>
/// A managed manufacturer lookup — an Arabic-name-only managed value, name only in
/// the first version (BR-CAT-007, REQ-CAT-013). Catalog owns its lifecycle: create,
/// rename (non-audited in the first version — BR-CAT-053/DEC-CAT-032), and
/// activate/deactivate (BR-CAT-052). There is no hard delete; deactivation is the
/// official way to retire a value (BR-CAT-051). Name uniqueness after Arabic
/// normalization is enforced at the persistence boundary. Mirrors the Categories
/// managed-data model (DEC-CTG-001/002) — a deliberate copy, not a shared abstraction.
/// </summary>
public sealed class Manufacturer
{
    private Manufacturer()
    {
        // EF Core materialization only.
        Name = string.Empty;
    }

    public Manufacturer(Guid id, string name)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name.Trim();
        IsActive = true;
    }

    public Guid Id { get; }

    public string Name { get; private set; }

    /// <summary>
    /// Whether the manufacturer can be chosen for a new product (BR-CAT-052). A newly
    /// created manufacturer is active. Deactivation never touches products that already
    /// reference it (BR-CAT-052 / DEC-CAT-032, option B per DEC-CTG-002).
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>Renames the manufacturer (REQ-CAT-013, BR-CAT-007); non-audited in the first version (BR-CAT-053).</summary>
    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    /// <summary>Returns the manufacturer to new-product selection (REQ-CAT-048).</summary>
    public void Activate() => IsActive = true;

    /// <summary>
    /// Hides the manufacturer from new-product selection (REQ-CAT-048, BR-CAT-052).
    /// Always allowed, even while products reference it (DEC-CAT-032, option B) —
    /// existing references are left untouched.
    /// </summary>
    public void Deactivate() => IsActive = false;
}
