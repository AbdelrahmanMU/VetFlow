namespace VetFlow.Application.Catalog.Commands.Common;

/// <summary>
/// A manufacturer write command that carries a name (create and rename). The shared
/// <see cref="ManufacturerNameCommandValidator{TCommand}"/> validates it once so the
/// rule is never duplicated (BR-CAT-007). Mirrors the category equivalent — a
/// deliberate copy across the module boundary (Categories owns its own version).
/// </summary>
public interface IManufacturerNameCommand
{
    string Name { get; }
}
