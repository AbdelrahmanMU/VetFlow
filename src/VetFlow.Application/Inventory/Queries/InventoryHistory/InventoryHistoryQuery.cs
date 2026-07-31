using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Queries.InventoryHistory;

/// <summary>
/// Inventory movement history (REQ-INV-005, reopened by DEC-INV-038): a read-only chronological
/// list of stock movements, newest first, with a stable tie-break so offset pagination cannot
/// repeat or drop a row (BR-INV-044).
///
/// <para>A pure projection <b>over the movement ledger</b> (REQ-INV-009) — the correction R2
/// required: the preserved design projected over <c>InventoryBatch</c>, which could never
/// represent consumption because consumption mutates <c>RemainingQuantity</c> without creating a
/// row. The screen shows exactly what was written and never derives a balance from the ledger
/// (BR-INV-063).</para>
///
/// <para><b>No filters in this slice</b> (BR-INV-044) — page and page size only.</para>
/// </summary>
public sealed record InventoryHistoryQuery : IQuery<PagedResult<InventoryHistoryItemDto>>
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Upper bound on the page number so the handler's Int32 offset
    /// <c>(Page - 1) * PageSize</c> can never overflow (STD-BE-027).
    /// </summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}
