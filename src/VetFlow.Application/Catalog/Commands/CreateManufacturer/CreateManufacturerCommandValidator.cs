using VetFlow.Application.Catalog.Commands.Common;

namespace VetFlow.Application.Catalog.Commands.CreateManufacturer;

/// <summary>Front-line validation of the create request — the shared name rules (BR-CAT-007), nothing extra.</summary>
public sealed class CreateManufacturerCommandValidator : ManufacturerNameCommandValidator<CreateManufacturerCommand>
{
}
