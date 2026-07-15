using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Queries.ProductList;

/// <summary>
/// The product list (screen S1): search by Arabic name, English name, or unit
/// barcode (BR-CAT-044, REQ-CAT-040), the Catalog-owned filters (BR-CAT-045),
/// whitelisted sorting, and offset pagination (ADR-0015 §5).
/// Stock-dependent filters (low stock, out of stock) are owned by the
/// Inventory/Monitoring modules and ship with them (owner ruling, Sprint 3 slice 1).
/// </summary>
public sealed record ProductListQuery : IQuery<PagedResult<ProductListItemDto>>
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 200;

    /// <summary>
    /// Upper bound on the page number. Derived so the handler's Int32 offset
    /// <c>(Page - 1) * PageSize</c> can never overflow (STD-BE-027): with
    /// <see cref="MaxPageSize"/> capped, the largest offset stays below Int32.MaxValue.
    /// </summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;

    public string? Search { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? ManufacturerId { get; init; }

    public Guid? NatureId { get; init; }

    public ProductStatusFilter? Status { get; init; }

    public bool? IsRefrigerated { get; init; }

    public bool? HasExpiration { get; init; }

    public bool? IsSplittable { get; init; }

    public bool? HasSellingPrice { get; init; }

    public ProductListSortField Sort { get; init; } = ProductListSortField.Name;

    public SortDirection Direction { get; init; } = SortDirection.Ascending;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}
