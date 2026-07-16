namespace VetFlow.Domain.Categories;

/// <summary>
/// A product category — a flat, single-level, Arabic-name-only managed value
/// (BR-CTG-001, BR-CAT-013). The Categories module owns its lifecycle: create,
/// rename (non-audited in the first version — BR-CTG-007/DEC-CTG-001), and
/// activate/deactivate (BR-CTG-004). There is no hard delete; deactivation is the
/// official way to retire a value (BR-CTG-006). Name uniqueness after Arabic
/// normalization (BR-CTG-003) is enforced at the persistence boundary.
/// </summary>
public sealed class Category
{
    private Category()
    {
        // EF Core materialization only.
        Name = string.Empty;
    }

    public Category(Guid id, string name)
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
    /// Whether the category can be chosen for a new product (BR-CTG-004). A newly
    /// created category is active. Deactivation never touches products that already
    /// reference it (BR-CTG-005 / DEC-CTG-002).
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>Renames the category (REQ-CTG-003, BR-CTG-002); non-audited in the first version (BR-CTG-007).</summary>
    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    /// <summary>Returns the category to new-product selection (REQ-CTG-004).</summary>
    public void Activate() => IsActive = true;

    /// <summary>
    /// Hides the category from new-product selection (REQ-CTG-004, BR-CTG-005).
    /// Always allowed, even while products reference it (DEC-CTG-002, option B) —
    /// existing references are left untouched.
    /// </summary>
    public void Deactivate() => IsActive = false;
}
