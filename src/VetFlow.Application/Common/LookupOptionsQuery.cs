namespace VetFlow.Application.Common;

/// <summary>
/// Shared shape of the managed-lookup option queries: an optional normalized
/// name search plus offset pagination (ADR-0015 §5).
/// </summary>
public abstract record LookupOptionsQuery : IQuery<PagedResult<LookupOptionDto>>
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 200;

    /// <summary>
    /// Upper bound on the page number so the handler's Int32 offset
    /// <c>(Page - 1) * PageSize</c> can never overflow (STD-BE-027).
    /// </summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}
