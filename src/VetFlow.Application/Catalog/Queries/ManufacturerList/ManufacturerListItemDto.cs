namespace VetFlow.Application.Catalog.Queries.ManufacturerList;

/// <summary>One row of the manufacturer management list (screen: الشركات المصنعة) — the name and its state (AC-CAT-047).</summary>
public sealed record ManufacturerListItemDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required bool IsActive { get; init; }
}
