using VetFlow.Application.Categories.Commands.Common;
using VetFlow.Application.Common;

namespace VetFlow.Application.Categories.Commands.CreateCategory;

/// <summary>
/// Create a category (REQ-CTG-002, WF): the Arabic name only (BR-CTG-001). The
/// new category is active (BR-CTG-004). The name must be unique after Arabic
/// normalization (BR-CTG-003) — enforced by the handler and the database. The
/// result is the new category id.
/// </summary>
public sealed record CreateCategoryCommand : ICommand<Guid>, ICategoryNameCommand
{
    public required string Name { get; init; }
}
