using VetFlow.Application.Catalog.Commands.Common;

namespace VetFlow.Application.Catalog.Commands.UpdateProduct;

/// <summary>
/// Front-line validation of the update request — the shared product-write rules
/// (REQ-CAT-043 / BR-CAT-009), with nothing extra. The id comes from the route,
/// not user input, so a missing product surfaces as a 404 from the handler, not a
/// field-level validation error.
/// </summary>
public sealed class UpdateProductCommandValidator : ProductWriteCommandValidator<UpdateProductCommand>
{
}
