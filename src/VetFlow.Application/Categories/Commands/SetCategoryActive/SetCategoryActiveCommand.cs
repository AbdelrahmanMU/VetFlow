using VetFlow.Application.Common;

namespace VetFlow.Application.Categories.Commands.SetCategoryActive;

/// <summary>
/// Activate or deactivate a category (REQ-CTG-004, BR-CTG-004). Deactivation is
/// always allowed, even while products reference it (DEC-CTG-002, option B); it is
/// the official retirement operation since there is no hard delete (BR-CTG-006).
/// The id comes from the route, so a missing category surfaces as a 404 (result
/// <c>null</c>).
/// </summary>
public sealed record SetCategoryActiveCommand : ICommand<Guid?>
{
    public required Guid Id { get; init; }

    public required bool IsActive { get; init; }
}
