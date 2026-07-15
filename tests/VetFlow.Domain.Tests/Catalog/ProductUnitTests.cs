using Shouldly;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Common;

namespace VetFlow.Domain.Tests.Catalog;

public sealed class ProductUnitTests
{
    [Fact]
    public void Selling_price_is_allowed_only_on_sale_units_BR_CAT_025()
    {
        var exception = Should.Throw<BusinessRuleException>(() =>
            new ProductUnit(Guid.NewGuid(), Guid.NewGuid(), 0, null, isPurchaseUnit: true, isSaleUnit: false, sellingPrice: 10m));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.PriceOnNonSaleUnit);
    }

    [Fact]
    public void Independent_non_proportional_prices_are_accepted_BR_CAT_025()
    {
        // TS-CAT-017: bottle 200 while the cm is 3 — deliberately non-proportional.
        var bottle = new ProductUnit(Guid.NewGuid(), Guid.NewGuid(), 0, 100, isPurchaseUnit: true, isSaleUnit: true, sellingPrice: 200m);
        var centimeter = new ProductUnit(Guid.NewGuid(), Guid.NewGuid(), 1, null, isPurchaseUnit: false, isSaleUnit: true, sellingPrice: 3m);

        bottle.SellingPrice.ShouldBe(200m);
        centimeter.SellingPrice.ShouldBe(3m);
    }

    [Fact]
    public void Each_unit_carries_at_most_one_optional_barcode_BR_CAT_024()
    {
        var withBarcode = new ProductUnit(Guid.NewGuid(), Guid.NewGuid(), 0, null, isPurchaseUnit: false, isSaleUnit: true, barcode: " 6221001 ");
        var withoutBarcode = new ProductUnit(Guid.NewGuid(), Guid.NewGuid(), 1, null, isPurchaseUnit: true, isSaleUnit: false);

        withBarcode.Barcode.ShouldBe("6221001");
        withoutBarcode.Barcode.ShouldBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Conversion_factor_must_be_positive_when_present_BR_CAT_016(decimal factor)
    {
        var exception = Should.Throw<BusinessRuleException>(() =>
            new ProductUnit(Guid.NewGuid(), Guid.NewGuid(), 0, factor, isPurchaseUnit: true, isSaleUnit: false));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.UnitProfileComposition);
    }

    [Fact]
    public void Conversion_factors_are_user_entered_and_stored_verbatim_BR_CAT_018()
    {
        var carton = new ProductUnit(Guid.NewGuid(), Guid.NewGuid(), 0, 12.5m, isPurchaseUnit: true, isSaleUnit: false);

        carton.QuantityInNextUnit.ShouldBe(12.5m);
    }
}
