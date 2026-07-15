namespace VetFlow.Api.Errors;

/// <summary>One published error contract entry (ADR-0018 §4).</summary>
public sealed record ErrorCatalogEntry
{
    public required string Code { get; init; }

    public required int Status { get; init; }

    /// <summary>Stable English title; the localized text is the Problem Details `detail`.</summary>
    public required string Title { get; init; }
}
