namespace VetFlow.Application.Categories.Queries.CategoryList;

/// <summary>One row of the category management list (screen: التصنيفات) — the name and its state (AC-CTG-001).</summary>
public sealed record CategoryListItemDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required bool IsActive { get; init; }
}
