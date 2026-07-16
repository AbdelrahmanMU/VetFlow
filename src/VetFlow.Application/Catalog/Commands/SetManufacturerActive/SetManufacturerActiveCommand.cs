using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Commands.SetManufacturerActive;

/// <summary>
/// Activate or deactivate a manufacturer (REQ-CAT-048, BR-CAT-052). Deactivation is
/// always allowed, even while products reference it (DEC-CAT-032, option B); it is
/// the official retirement operation since there is no hard delete (BR-CAT-051). The
/// id comes from the route, so a missing manufacturer surfaces as a 404 (result
/// <c>null</c>).
/// </summary>
public sealed record SetManufacturerActiveCommand : ICommand<Guid?>
{
    public required Guid Id { get; init; }

    public required bool IsActive { get; init; }
}
