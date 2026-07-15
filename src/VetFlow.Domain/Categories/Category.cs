namespace VetFlow.Domain.Categories;

/// <summary>
/// A product category — a flat, single-level list in the first version
/// (BR-CAT-013). This is the minimal scaffold the approved Catalog docs
/// require; category management itself belongs to the Categories module and
/// waits for that module's documentation (owner ruling, Sprint 3 slice 1).
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
    }

    public Guid Id { get; }

    public string Name { get; private set; }
}
