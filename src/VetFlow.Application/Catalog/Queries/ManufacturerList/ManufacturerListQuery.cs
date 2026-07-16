using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Queries.ManufacturerList;

/// <summary>
/// The manufacturer management list (REQ-CAT-047, AC-CAT-047): search by normalized
/// Arabic name, whitelisted sorting (name / status), and offset pagination
/// (ADR-0015 §5). The list shows both active and inactive manufacturers so a value
/// can be reactivated from it (AC-CAT-048) — the new-product selector filtering to
/// active is a separate concern (REQ-CAT-048 / DEC-CAT-032). Repurposes the former
/// options endpoint: the item shape is a superset {id, name, isActive} so the
/// product-list filter and editor consumers keep working (mirrors DEC for categories).
/// </summary>
public sealed record ManufacturerListQuery : IQuery<PagedResult<ManufacturerListItemDto>>
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

    public ManufacturerListSortField Sort { get; init; } = ManufacturerListSortField.Name;

    public SortDirection Direction { get; init; } = SortDirection.Ascending;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}
