using VetFlow.Application.Categories.Commands.Common;

namespace VetFlow.Application.Categories.Commands.RenameCategory;

/// <summary>Front-line validation of the rename request — the shared name rules (BR-CTG-002), nothing extra.</summary>
public sealed class RenameCategoryCommandValidator : CategoryNameCommandValidator<RenameCategoryCommand>
{
}
