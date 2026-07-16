using VetFlow.Application.Common;

namespace VetFlow.Application.Categories.Queries.CategoryList;

/// <summary>
/// The category management list (REQ-CTG-001, AC-CTG-001): search by normalized
/// Arabic name, whitelisted sorting (name / status), and offset pagination
/// (ADR-0015 §5). The list shows both active and inactive categories so a value
/// can be reactivated from it (AC-CTG-004) — the new-product selector filters to
/// active is a separate concern (REQ-CTG-005).
/// </summary>
public sealed record CategoryListQuery : IQuery<PagedResult<CategoryListItemDto>>
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

    public CategoryListSortField Sort { get; init; } = CategoryListSortField.Name;

    public SortDirection Direction { get; init; } = SortDirection.Ascending;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}
