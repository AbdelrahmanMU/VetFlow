using VetFlow.Application.Catalog.Commands.Common;

namespace VetFlow.Application.Catalog.Commands.CreateProduct;

/// <summary>
/// Front-line validation of the create request (REQ-CAT-043, AC-CAT-043) — the
/// shared product-write rules (BR-CAT-009), with nothing extra. The internal code
/// is system-generated (DEC-CAT-026) and never validated as user input.
/// </summary>
public sealed class CreateProductCommandValidator : ProductWriteCommandValidator<CreateProductCommand>
{
}
