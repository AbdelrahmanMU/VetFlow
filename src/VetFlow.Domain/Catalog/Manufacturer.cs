namespace VetFlow.Domain.Catalog;

/// <summary>
/// A managed manufacturer lookup — name only in the first version (BR-CAT-007).
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
    }

    public Guid Id { get; }

    public string Name { get; private set; }
}
