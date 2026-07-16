namespace VetFlow.Application.Categories.Commands.Common;

/// <summary>
/// A category write command that carries a name (create and rename). The shared
/// <see cref="CategoryNameCommandValidator{TCommand}"/> validates it once so the
/// rule is never duplicated (BR-CTG-002).
/// </summary>
public interface ICategoryNameCommand
{
    string Name { get; }
}
