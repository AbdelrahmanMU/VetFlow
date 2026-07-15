namespace VetFlow.Domain.Catalog;

/// <summary>
/// A user-managed measurement/packaging unit; the system ships a default common
/// set and users may add more (BR-CAT-017).
/// </summary>
public sealed class Unit
{
    private Unit()
    {
        // EF Core materialization only.
        Name = string.Empty;
    }

    public Unit(Guid id, string name)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = id;
        Name = name.Trim();
    }

    public Guid Id { get; }

    public string Name { get; private set; }
}
