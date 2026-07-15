using VetFlow.Domain.Common;

namespace VetFlow.Domain.Catalog;

/// <summary>
/// One row of a product's unit profile (BR-CAT-016): the unit, its user-entered
/// conversion factor to the next unit in the chain (BR-CAT-018 — never derived
/// by the system), its roles, an optional single barcode (BR-CAT-024), and an
/// independent manual selling price for sale units (BR-CAT-025).
/// </summary>
public sealed class ProductUnit
{
    private ProductUnit()
    {
        // EF Core materialization only.
    }

    public ProductUnit(
        Guid id,
        Guid unitId,
        int position,
        decimal? quantityInNextUnit,
        bool isPurchaseUnit,
        bool isSaleUnit,
        string? barcode = null,
        decimal? sellingPrice = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(unitId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfNegative(position);

        if (quantityInNextUnit is <= 0)
        {
            throw new BusinessRuleException(
                CatalogErrorCodes.UnitProfileComposition,
                new Dictionary<string, string> { ["reason"] = "conversionFactorNotPositive" });
        }

        if (sellingPrice is not null && !isSaleUnit)
        {
            throw new BusinessRuleException(CatalogErrorCodes.PriceOnNonSaleUnit);
        }

        Id = id;
        UnitId = unitId;
        Position = position;
        QuantityInNextUnit = quantityInNextUnit;
        IsPurchaseUnit = isPurchaseUnit;
        IsSaleUnit = isSaleUnit;
        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        SellingPrice = sellingPrice;
    }

    public Guid Id { get; }

    public Guid UnitId { get; }

    /// <summary>Order in the unit chain, 0 = largest packaging level (BR-CAT-019).</summary>
    public int Position { get; }

    /// <summary>
    /// User-entered quantity of the next (smaller) unit contained in this unit,
    /// e.g. carton = 12 boxes. Null on the smallest unit of the chain.
    /// </summary>
    public decimal? QuantityInNextUnit { get; }

    public bool IsPurchaseUnit { get; }

    public bool IsSaleUnit { get; }

    /// <summary>One optional barcode per unit (BR-CAT-024).</summary>
    public string? Barcode { get; }

    /// <summary>Independent manual selling price in EGP; sale units only (BR-CAT-025).</summary>
    public decimal? SellingPrice { get; }
}
