using VetFlow.Application.Catalog.Commands.Common;
using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Commands.RenameManufacturer;

/// <summary>
/// Rename a manufacturer (REQ-CAT-013) — same validity and uniqueness rules as
/// creation (BR-CAT-007), non-audited in the first version (BR-CAT-053). The id
/// comes from the route, so a missing manufacturer surfaces as a 404 from the
/// handler (result <c>null</c>), not a field error — mirroring the product edit.
/// </summary>
public sealed record RenameManufacturerCommand : ICommand<Guid?>, IManufacturerNameCommand
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }
}
