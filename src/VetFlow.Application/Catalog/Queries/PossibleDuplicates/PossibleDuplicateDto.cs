namespace VetFlow.Application.Catalog.Queries.PossibleDuplicates;

/// <summary>
/// A product the system flags as a possible duplicate of what the user is
/// creating (DEC-CAT-027): similar Arabic name and the same manufacturer. The
/// four identity elements (BR-CAT-001) are returned so the UI can show a
/// side-by-side comparison (ui.md §5). Advisory only — never blocks (BR-CAT-042).
/// </summary>
public sealed record PossibleDuplicateDto
{
    public required Guid Id { get; init; }

    public required string ArabicName { get; init; }

    public string? EnglishName { get; init; }

    public string? Size { get; init; }

    public string? Concentration { get; init; }

    public required string ManufacturerName { get; init; }
}
