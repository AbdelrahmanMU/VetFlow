using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Queries.InventoryProjection;

/// <summary>
/// The inventory projection (REQ-INV-002): a read-only view of the current
/// physical inventory — one row per product that has an on-hand record
/// (BR-INV-007). Search by product name, filter by category, "expiring soon"
/// (BR-INV-013) and "out of stock" (BR-INV-011), whitelisted sorting (BR-INV-014),
/// and offset pagination. Mirrors the Catalog product-list pattern; the default
/// order is the product name ascending.
/// </summary>
public sealed record InventoryProjectionQuery : IQuery<PagedResult<InventoryProjectionItemDto>>
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 200;

    /// <summary>
    /// Upper bound on the page number so the handler's Int32 offset
    /// <c>(Page - 1) * PageSize</c> can never overflow (STD-BE-027).
    /// </summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;

    /// <summary>Fixed "expiring soon" horizon — 30 calendar days (BR-INV-013, DEC-INV-005).</summary>
    public const int ExpiringSoonHorizonDays = 30;

    public string? Search { get; init; }

    public Guid? CategoryId { get; init; }

    /// <summary>When true, keep only products whose nearest expiry is within the horizon (BR-INV-013).</summary>
    public bool? ExpiringSoon { get; init; }

    /// <summary>When true, keep only products whose on-hand quantity is zero (BR-INV-011).</summary>
    public bool? OutOfStock { get; init; }

    public InventoryProjectionSortField Sort { get; init; } = InventoryProjectionSortField.Product;

    public SortDirection Direction { get; init; } = SortDirection.Ascending;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}
