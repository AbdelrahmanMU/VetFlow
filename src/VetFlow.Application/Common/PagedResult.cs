namespace VetFlow.Application.Common;

/// <summary>
/// The fixed collection envelope of the API contract (ADR-0015 §5):
/// items, page, pageSize, totalCount.
/// </summary>
public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }
}
