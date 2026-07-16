using VetFlow.Application.Categories.Commands.Common;

namespace VetFlow.Application.Categories.Commands.CreateCategory;

/// <summary>Front-line validation of the create request — the shared name rules (BR-CTG-002), nothing extra.</summary>
public sealed class CreateCategoryCommandValidator : CategoryNameCommandValidator<CreateCategoryCommand>
{
}
