using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Queries.ProductList;

/// <summary>
/// One row of the product list (screen S1): identity, classification, the
/// stock-keeping unit, the price of the default sale unit, status, and the
/// capability flags shown as silent icons.
/// </summary>
public sealed record ProductListItemDto
{
    public required Guid Id { get; init; }

    public required string ArabicName { get; init; }

    public string? EnglishName { get; init; }

    public string? Size { get; init; }

    public string? Concentration { get; init; }

    public required string CategoryName { get; init; }

    public required string ManufacturerName { get; init; }

    public required string NatureName { get; init; }

    public required string StorageUnitName { get; init; }

    public required string DefaultSaleUnitName { get; init; }

    public MoneyDto? DefaultSaleUnitPrice { get; init; }

    public required ProductStatusDto Status { get; init; }

    public required bool IsSplittable { get; init; }

    public required bool IsRefrigerated { get; init; }

    public required bool HasExpiration { get; init; }

    /// <summary>False renders the «بلا سعر» badge — the product exists but cannot be sold (BR-CAT-026).</summary>
    public required bool HasSellingPrice { get; init; }
}
