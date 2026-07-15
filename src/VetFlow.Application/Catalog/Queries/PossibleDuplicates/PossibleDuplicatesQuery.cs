using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Queries.PossibleDuplicates;

/// <summary>
/// The possible-duplicate advisory read (REQ-CAT-042, DEC-CAT-027): the UI runs
/// it at save time and surfaces a warning the user may override — it never
/// blocks the write (BR-CAT-042 / DEC-CAT-018). A match is a similar normalized
/// Arabic name AND the same manufacturer; size/concentration are not required to
/// trigger. The envelope is the fixed collection shape (STD-API-022).
/// </summary>
public sealed record PossibleDuplicatesQuery : IQuery<PagedResult<PossibleDuplicateDto>>
{
    /// <summary>
    /// Initial trigram-similarity threshold (DEC-CAT-027): a named, tunable
    /// constant — engineering may recalibrate it without changing the rule.
    /// </summary>
    public const double SimilarityThreshold = 0.4;

    /// <summary>Upper bound on the advisory list — a warning, not a full search.</summary>
    public const int MaxResults = 10;

    public required string ArabicName { get; init; }

    public required Guid ManufacturerId { get; init; }
}
