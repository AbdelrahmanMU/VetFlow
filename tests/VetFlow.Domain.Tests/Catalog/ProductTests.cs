using Shouldly;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Common;

namespace VetFlow.Domain.Tests.Catalog;

public sealed class ProductTests
{
    [Fact]
    public void Product_is_created_active_with_the_documented_minimum_BR_CAT_009()
    {
        var product = ProductTestData.ValidProduct();

        product.Status.ShouldBe(ProductStatus.Active);
        product.ArabicName.ShouldBe("أموكسيسيلين 500");
        product.Units.Count.ShouldBe(4);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Product_cannot_be_created_without_arabic_name_BR_CAT_009(string arabicName)
    {
        var exception = Should.Throw<BusinessRuleException>(() => ProductTestData.ValidProduct(arabicName: arabicName));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.MinimumProductData);
        exception.Metadata["field"].ShouldBe("arabicName");
    }

    [Fact]
    public void Product_cannot_be_created_without_category_BR_CAT_009()
    {
        var exception = Should.Throw<BusinessRuleException>(() =>
            ProductTestData.ValidProduct(categoryId: Guid.Empty));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.MinimumProductData);
        exception.Metadata["field"].ShouldBe("category");
    }

    [Fact]
    public void Product_cannot_be_created_without_manufacturer_BR_CAT_014_and_BR_CAT_009()
    {
        var exception = Should.Throw<BusinessRuleException>(() =>
            ProductTestData.ValidProduct(manufacturerId: Guid.Empty));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.MinimumProductData);
        exception.Metadata["field"].ShouldBe("manufacturer");
    }

    [Fact]
    public void Product_cannot_be_created_without_a_product_nature_BR_CAT_014()
    {
        var exception = Should.Throw<BusinessRuleException>(() =>
            ProductTestData.ValidProduct(natureId: Guid.Empty));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.MinimumProductData);
        exception.Metadata["field"].ShouldBe("nature");
    }

    [Fact]
    public void Product_requires_at_least_one_purchase_unit_BR_CAT_009()
    {
        var saleOnly = new List<ProductUnit>
        {
            new(Guid.NewGuid(), ProductTestData.PillUnitId, 0, null, isPurchaseUnit: false, isSaleUnit: true),
        };

        var exception = Should.Throw<BusinessRuleException>(() => ProductTestData.ValidProduct(
            units: saleOnly,
            storageUnitId: ProductTestData.PillUnitId,
            defaultSaleUnitId: ProductTestData.PillUnitId,
            defaultPurchaseUnitId: ProductTestData.PillUnitId));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.MinimumProductData);
        exception.Metadata["field"].ShouldBe("purchaseUnit");
    }

    [Fact]
    public void Product_requires_at_least_one_sale_unit_BR_CAT_009()
    {
        var purchaseOnly = new List<ProductUnit>
        {
            new(Guid.NewGuid(), ProductTestData.CartonUnitId, 0, null, isPurchaseUnit: true, isSaleUnit: false),
        };

        var exception = Should.Throw<BusinessRuleException>(() => ProductTestData.ValidProduct(
            units: purchaseOnly,
            storageUnitId: ProductTestData.CartonUnitId,
            defaultSaleUnitId: ProductTestData.CartonUnitId,
            defaultPurchaseUnitId: ProductTestData.CartonUnitId));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.MinimumProductData);
        exception.Metadata["field"].ShouldBe("saleUnit");
    }

    [Fact]
    public void Product_belongs_to_exactly_one_category_BR_CAT_012()
    {
        var categoryId = Guid.NewGuid();

        var product = ProductTestData.ValidProduct(categoryId: categoryId);

        product.CategoryId.ShouldBe(categoryId);
    }

    [Fact]
    public void Unit_profile_rejects_duplicate_units_BR_CAT_016()
    {
        var duplicated = new List<ProductUnit>
        {
            new(Guid.NewGuid(), ProductTestData.BoxUnitId, 0, 10, isPurchaseUnit: true, isSaleUnit: false),
            new(Guid.NewGuid(), ProductTestData.BoxUnitId, 1, null, isPurchaseUnit: false, isSaleUnit: true),
        };

        var exception = Should.Throw<BusinessRuleException>(() => ProductTestData.ValidProduct(
            units: duplicated,
            storageUnitId: ProductTestData.BoxUnitId,
            defaultSaleUnitId: ProductTestData.BoxUnitId,
            defaultPurchaseUnitId: ProductTestData.BoxUnitId));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.UnitProfileComposition);
    }

    [Fact]
    public void Storage_unit_must_be_one_of_the_profile_units_BR_CAT_020()
    {
        var exception = Should.Throw<BusinessRuleException>(() =>
            ProductTestData.ValidProduct(storageUnitId: Guid.NewGuid()));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.StorageUnitNotInProfile);
    }

    [Fact]
    public void Default_sale_unit_must_come_from_the_profile_BR_CAT_022()
    {
        var exception = Should.Throw<BusinessRuleException>(() =>
            ProductTestData.ValidProduct(defaultSaleUnitId: Guid.NewGuid()));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.DefaultSaleUnitInvalid);
    }

    [Fact]
    public void Default_sale_unit_must_be_a_sale_unit_BR_CAT_022()
    {
        // The carton is a purchase-only unit in the default chain.
        var exception = Should.Throw<BusinessRuleException>(() =>
            ProductTestData.ValidProduct(defaultSaleUnitId: ProductTestData.CartonUnitId));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.DefaultSaleUnitInvalid);
    }

    [Fact]
    public void Default_purchase_unit_must_be_a_purchase_unit_from_the_profile_BR_CAT_021()
    {
        // The pill is a sale-only unit in the default chain.
        var exception = Should.Throw<BusinessRuleException>(() =>
            ProductTestData.ValidProduct(defaultPurchaseUnitId: ProductTestData.PillUnitId));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.DefaultPurchaseUnitInvalid);
    }

    [Fact]
    public void Multi_level_unit_chain_with_per_level_roles_is_supported_BR_CAT_019()
    {
        var product = ProductTestData.ValidProduct();

        product.Units.Single(unit => unit.UnitId == ProductTestData.CartonUnitId).IsPurchaseUnit.ShouldBeTrue();
        product.Units.Count(unit => unit.IsSaleUnit).ShouldBe(3);
        product.StorageUnitId.ShouldBe(ProductTestData.PillUnitId);
    }

    [Fact]
    public void Optional_identity_fields_may_stay_empty_BR_CAT_008()
    {
        var product = ProductTestData.ValidProduct();

        product.EnglishName.ShouldBeNull();
        product.Size.ShouldBeNull();
        product.Concentration.ShouldBeNull();
    }
}
