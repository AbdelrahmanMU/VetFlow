namespace VetFlow.Domain.Catalog;

/// <summary>
/// The mandatory, extensible product nature — the property system behavior is
/// built on, independent of the category (BR-CAT-014, BR-CAT-015).
/// </summary>
public sealed class ProductNature
{
    private ProductNature()
    {
        // EF Core materialization only.
        Name = string.Empty;
    }

    public ProductNature(Guid id, string name)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name.Trim();
    }

    public Guid Id { get; }

    public string Name { get; private set; }
}
