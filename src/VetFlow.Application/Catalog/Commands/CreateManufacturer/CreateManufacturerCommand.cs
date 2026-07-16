using VetFlow.Application.Catalog.Commands.Common;
using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Commands.CreateManufacturer;

/// <summary>
/// Create a manufacturer (REQ-CAT-013): the Arabic name only (BR-CAT-007). The new
/// manufacturer is active (BR-CAT-052). The name must be unique after Arabic
/// normalization — enforced by the handler and the database. The result is the new
/// manufacturer id.
/// </summary>
public sealed record CreateManufacturerCommand : ICommand<Guid>, IManufacturerNameCommand
{
    public required string Name { get; init; }
}
