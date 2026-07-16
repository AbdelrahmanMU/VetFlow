using VetFlow.Application.Catalog.Commands.Common;

namespace VetFlow.Application.Catalog.Commands.RenameManufacturer;

/// <summary>Front-line validation of the rename request — the shared name rules (BR-CAT-007), nothing extra.</summary>
public sealed class RenameManufacturerCommandValidator : ManufacturerNameCommandValidator<RenameManufacturerCommand>
{
}
