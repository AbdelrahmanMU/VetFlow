using VetFlow.Application.Catalog.Queries.ProductList;
using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Queries.ProductDetails;

/// <summary>
/// The full read model of one product (screen S2, WF-CAT-003 read side):
/// identity incl. the internal code (REQ-CAT-008, for display/support only —
/// DEC-CAT-016), capabilities, the unit profile with its distinguished units
/// (REQ-CAT-015), prices, and internal notes (REQ-CAT-005).
/// </summary>
public sealed record ProductDetailsDto
{
    public required Guid Id { get; init; }

    public required string InternalCode { get; init; }

    public required string ArabicName { get; init; }

    public string? EnglishName { get; init; }

    public string? Size { get; init; }

    public string? Concentration { get; init; }

    public required string CategoryName { get; init; }

    public required string ManufacturerName { get; init; }

    public required string NatureName { get; init; }

    public required ProductStatusDto Status { get; init; }

    public required bool IsSplittable { get; init; }

    public required bool IsRefrigerated { get; init; }

    public required bool HasExpiration { get; init; }

    public required bool HasOpenExpiration { get; init; }

    public int? OpenExpirationPeriodDays { get; init; }

    public string? InternalNotes { get; init; }

    /// <summary>False ⇒ the product cannot be sold until a price is recorded (REQ-CAT-025).</summary>
    public required bool HasSellingPrice { get; init; }

    public required IReadOnlyList<ProductUnitDetailsDto> Units { get; init; }
}

/// <summary>One unit-profile row in the details read model (BR-CAT-016).</summary>
public sealed record ProductUnitDetailsDto
{
    public required Guid UnitId { get; init; }

    public required string UnitName { get; init; }

    public required int Position { get; init; }

    public decimal? QuantityInNextUnit { get; init; }

    public required bool IsPurchaseUnit { get; init; }

    public required bool IsSaleUnit { get; init; }

    public string? Barcode { get; init; }

    public MoneyDto? SellingPrice { get; init; }

    /// <summary>The single stock-keeping unit — all quantities compute in it (REQ-CAT-019).</summary>
    public required bool IsStorageUnit { get; init; }

    public required bool IsDefaultSaleUnit { get; init; }

    public required bool IsDefaultPurchaseUnit { get; init; }
}
