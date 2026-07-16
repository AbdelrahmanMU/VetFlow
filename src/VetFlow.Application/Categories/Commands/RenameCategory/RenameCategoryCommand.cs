using VetFlow.Application.Categories.Commands.Common;
using VetFlow.Application.Common;

namespace VetFlow.Application.Categories.Commands.RenameCategory;

/// <summary>
/// Rename a category (REQ-CTG-003) — same validity and uniqueness rules as
/// creation (BR-CTG-002/003), non-audited in the first version (BR-CTG-007). The
/// id comes from the route, so a missing category surfaces as a 404 from the
/// handler (result <c>null</c>), not a field error — mirroring the product edit.
/// </summary>
public sealed record RenameCategoryCommand : ICommand<Guid?>, ICategoryNameCommand
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }
}
