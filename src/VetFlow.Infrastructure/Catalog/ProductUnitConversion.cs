using VetFlow.Application.Catalog.Queries.ProductDetails;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>Why a quantity could not be converted into the product's stock unit.</summary>
internal enum UnitConversionFailure
{
    None = 0,

    /// <summary>The product's unit profile has no stock-keeping unit (BR-CAT-020).</summary>
    NoStorageUnit = 1,

    /// <summary>The given unit is not part of the product's unit profile (BR-CAT-016).</summary>
    UnitNotInProfile = 2,

    /// <summary>A conversion factor is missing somewhere in the chain (BR-CAT-018/019).</summary>
    MissingConversionFactor = 3,
}

/// <summary>
/// Converting a quantity from one of a product's units into its canonical <b>stock unit</b>, using
/// the Catalog unit profile's existing conversion factors (BR-CAT-018/019) — an existing Catalog
/// capability, not a new business rule (owner ruling 2026-07-22). The factor-to-smallest of a unit
/// is the product of the chain's <c>QuantityInNextUnit</c> values from that unit down to the
/// smallest; the stock quantity is <c>quantity × factor(unit) ÷ factor(stockUnit)</c>, correct
/// whichever unit is the larger. Quantities are not money — no rounding policy applies to them
/// (BR-SAL-007, BR-INV-058).
///
/// Shared by the two inventory movements — receiving (BR-PUR-010) and consumption (BR-SAL-010) —
/// so the conversion exists once, in the module that owns unit profiles. Each caller maps the
/// failure to its own module's error code.
/// </summary>
internal static class ProductUnitConversion
{
    /// <summary>
    /// Convert <paramref name="quantity"/> of <paramref name="unitId"/> into the product's stock
    /// unit. Returns <see cref="UnitConversionFailure.None"/> on success.
    ///
    /// <paramref name="isExact"/> reports whether the conversion produced a value with <b>no
    /// residue</b> — that is, whether multiplying the result back by the stock-unit factor returns
    /// the original amount. It is always true when the stock unit is the smallest measurable unit,
    /// which the Catalog now requires (BR-CAT-020 as amended by DEC-CAT-033); it can only be false
    /// for a product configured against that rule. Consumption <b>rejects</b> an inexact
    /// conversion rather than rounding it (BR-INV-058, BR-SAL-012, AC-SAL-013).
    /// </summary>
    internal static UnitConversionFailure TryConvertToStockUnit(
        ProductDetailsDto product,
        Guid unitId,
        decimal quantity,
        out decimal stockQuantity,
        out bool isExact)
    {
        stockQuantity = 0m;
        isExact = false;

        var chain = product.Units.OrderBy(unit => unit.Position).ToList();

        var storageIndex = chain.FindIndex(unit => unit.IsStorageUnit);
        if (storageIndex < 0)
        {
            return UnitConversionFailure.NoStorageUnit;
        }

        var sourceIndex = chain.FindIndex(unit => unit.UnitId == unitId);
        if (sourceIndex < 0)
        {
            return UnitConversionFailure.UnitNotInProfile;
        }

        if (!TryFactorToSmallest(chain, sourceIndex, out var sourceFactor)
            || !TryFactorToSmallest(chain, storageIndex, out var storageFactor))
        {
            return UnitConversionFailure.MissingConversionFactor;
        }

        var inSmallestUnit = quantity * sourceFactor;
        stockQuantity = inSmallestUnit / storageFactor;
        // Exact ⇔ nothing was lost dividing by the stock-unit factor. Multiplying back is the
        // decimal-safe test: it accepts a genuinely divisible amount (and any quantity at all when
        // the stock unit is the smallest, where the factor is 1) and rejects a repeating quotient.
        isExact = stockQuantity * storageFactor == inSmallestUnit;

        return UnitConversionFailure.None;
    }

    /// <summary>Product of the chain's conversion factors from <paramref name="index"/> down to the smallest unit (1 for the smallest).</summary>
    private static bool TryFactorToSmallest(IReadOnlyList<ProductUnitDetailsDto> chain, int index, out decimal factor)
    {
        factor = 1m;
        for (var step = index; step < chain.Count - 1; step++)
        {
            if (chain[step].QuantityInNextUnit is not { } quantityInNextUnit)
            {
                return false;
            }

            factor *= quantityInNextUnit;
        }

        return true;
    }
}
