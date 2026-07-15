using Shouldly;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Common;

namespace VetFlow.Domain.Tests.Catalog;

/// <summary>
/// The non-audited basic edit (REQ-CAT-038 first clause / BR-CAT-041, REQ-CAT-003,
/// DEC-CAT-031): editing basic data and the unit profile is free, re-enforces the
/// same invariants as creation, never mutates the internal code, status, or
/// selling prices.
/// </summary>
public sealed class ProductUpdateTests
{
    private static List<ProductUnitDraft> DefaultDrafts() =>
    [
        new(ProductTestData.CartonUnitId, 0, 12, IsPurchaseUnit: true, IsSaleUnit: false, Barcode: null),
        new(ProductTestData.BoxUnitId, 1, 10, IsPurchaseUnit: false, IsSaleUnit: true, Barcode: null),
        new(ProductTestData.StripUnitId, 2, 10, IsPurchaseUnit: false, IsSaleUnit: true, Barcode: "6221000000012"),
        new(ProductTestData.PillUnitId, 3, null, IsPurchaseUnit: false, IsSaleUnit: true, Barcode: null),
    ];

    private static void UpdateWith(
        Product product,
        string arabicName = "اسم محدث",
        string? englishName = null,
        string? size = null,
        string? concentration = null,
        Guid? categoryId = null,
        Guid? manufacturerId = null,
        Guid? natureId = null,
        ProductCapabilities? capabilities = null,
        string? internalNotes = null,
        IReadOnlyCollection<ProductUnitDraft>? drafts = null,
        Guid? storageUnitId = null,
        Guid? defaultSaleUnitId = null,
        Guid? defaultPurchaseUnitId = null) =>
        product.Update(
            arabicName,
            englishName,
            size,
            concentration,
            categoryId ?? Guid.NewGuid(),
            manufacturerId ?? Guid.NewGuid(),
            natureId ?? Guid.NewGuid(),
            capabilities ?? ProductTestData.DefaultCapabilities(),
            internalNotes,
            drafts ?? DefaultDrafts(),
            storageUnitId ?? ProductTestData.PillUnitId,
            defaultSaleUnitId ?? ProductTestData.StripUnitId,
            defaultPurchaseUnitId ?? ProductTestData.CartonUnitId);

    [Fact]
    public void Update_edits_basic_data_freely_BR_CAT_041_and_REQ_CAT_003()
    {
        var product = ProductTestData.ValidProduct();
        var category = Guid.NewGuid();

        UpdateWith(
            product,
            arabicName: "أموكسيسيلين 250",
            englishName: "Amoxicillin 250",
            size: "250mg",
            concentration: "250",
            categoryId: category,
            internalNotes: "  يوزّع بحذر  ");

        product.ArabicName.ShouldBe("أموكسيسيلين 250");
        product.EnglishName.ShouldBe("Amoxicillin 250");
        product.Size.ShouldBe("250mg");
        product.Concentration.ShouldBe("250");
        product.CategoryId.ShouldBe(category);
        product.InternalNotes.ShouldBe("يوزّع بحذر");
    }

    [Fact]
    public void Update_never_changes_the_internal_code_or_status_DEC_CAT_026()
    {
        var product = ProductTestData.ValidProduct(internalCode: "PRD-000077");

        UpdateWith(product);

        product.InternalCode.ShouldBe("PRD-000077");
        product.Status.ShouldBe(ProductStatus.Active);
    }

    [Fact]
    public void Update_preserves_selling_prices_of_retained_units_by_unit_DEC_CAT_031()
    {
        // The default chain seeds Box=120, Strip=15, Pill=2 (ProductTestData).
        var product = ProductTestData.ValidProduct();

        UpdateWith(product, arabicName: "اسم جديد فقط");

        product.Units.Single(unit => unit.UnitId == ProductTestData.BoxUnitId).SellingPrice.ShouldBe(120m);
        product.Units.Single(unit => unit.UnitId == ProductTestData.StripUnitId).SellingPrice.ShouldBe(15m);
        product.Units.Single(unit => unit.UnitId == ProductTestData.PillUnitId).SellingPrice.ShouldBe(2m);
    }

    [Fact]
    public void Update_gives_a_newly_added_unit_no_price_DEC_CAT_031()
    {
        var product = ProductTestData.ValidProduct();
        var newUnitId = Guid.NewGuid();

        var drafts = DefaultDrafts();
        drafts.Add(new ProductUnitDraft(newUnitId, 4, null, IsPurchaseUnit: false, IsSaleUnit: true, Barcode: null));

        UpdateWith(product, drafts: drafts);

        product.Units.Single(unit => unit.UnitId == newUnitId).SellingPrice.ShouldBeNull();
    }

    [Fact]
    public void Update_drops_the_price_when_a_unit_stops_being_a_sale_unit_BR_CAT_025()
    {
        var product = ProductTestData.ValidProduct();

        // Box was a priced sale unit; make it purchase-only. A non-sale unit cannot
        // hold a price, so the price is dropped rather than throwing.
        var drafts = new List<ProductUnitDraft>
        {
            new(ProductTestData.CartonUnitId, 0, 12, IsPurchaseUnit: true, IsSaleUnit: false, Barcode: null),
            new(ProductTestData.BoxUnitId, 1, 10, IsPurchaseUnit: true, IsSaleUnit: false, Barcode: null),
            new(ProductTestData.StripUnitId, 2, 10, IsPurchaseUnit: false, IsSaleUnit: true, Barcode: null),
            new(ProductTestData.PillUnitId, 3, null, IsPurchaseUnit: false, IsSaleUnit: true, Barcode: null),
        };

        UpdateWith(
            product,
            drafts: drafts,
            defaultPurchaseUnitId: ProductTestData.CartonUnitId,
            defaultSaleUnitId: ProductTestData.StripUnitId);

        product.Units.Single(unit => unit.UnitId == ProductTestData.BoxUnitId).SellingPrice.ShouldBeNull();
    }

    [Fact]
    public void Update_replaces_the_unit_profile_and_removes_dropped_units_BR_CAT_016()
    {
        var product = ProductTestData.ValidProduct();

        // Collapse to a two-level chain: strip (purchase) → pill (sale).
        var drafts = new List<ProductUnitDraft>
        {
            new(ProductTestData.StripUnitId, 0, 10, IsPurchaseUnit: true, IsSaleUnit: false, Barcode: null),
            new(ProductTestData.PillUnitId, 1, null, IsPurchaseUnit: false, IsSaleUnit: true, Barcode: null),
        };

        UpdateWith(
            product,
            drafts: drafts,
            storageUnitId: ProductTestData.PillUnitId,
            defaultSaleUnitId: ProductTestData.PillUnitId,
            defaultPurchaseUnitId: ProductTestData.StripUnitId);

        product.Units.Count.ShouldBe(2);
        product.Units.ShouldNotContain(unit => unit.UnitId == ProductTestData.CartonUnitId);
        product.Units.ShouldNotContain(unit => unit.UnitId == ProductTestData.BoxUnitId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_re_enforces_the_arabic_name_minimum_BR_CAT_009(string arabicName)
    {
        var product = ProductTestData.ValidProduct();

        var exception = Should.Throw<BusinessRuleException>(() => UpdateWith(product, arabicName: arabicName));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.MinimumProductData);
        exception.Metadata["field"].ShouldBe("arabicName");
    }

    [Fact]
    public void Update_re_enforces_at_least_one_sale_unit_BR_CAT_009()
    {
        var product = ProductTestData.ValidProduct();

        var purchaseOnly = new List<ProductUnitDraft>
        {
            new(ProductTestData.CartonUnitId, 0, null, IsPurchaseUnit: true, IsSaleUnit: false, Barcode: null),
        };

        var exception = Should.Throw<BusinessRuleException>(() => UpdateWith(
            product,
            drafts: purchaseOnly,
            storageUnitId: ProductTestData.CartonUnitId,
            defaultSaleUnitId: ProductTestData.CartonUnitId,
            defaultPurchaseUnitId: ProductTestData.CartonUnitId));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.MinimumProductData);
        exception.Metadata["field"].ShouldBe("saleUnit");
    }

    [Fact]
    public void Update_re_enforces_storage_unit_membership_BR_CAT_020()
    {
        var product = ProductTestData.ValidProduct();

        var exception = Should.Throw<BusinessRuleException>(() =>
            UpdateWith(product, storageUnitId: Guid.NewGuid()));

        exception.ErrorCode.ShouldBe(CatalogErrorCodes.StorageUnitNotInProfile);
    }

    [Fact]
    public void Update_rejects_a_failed_edit_without_mutating_state_BR_CAT_041()
    {
        var product = ProductTestData.ValidProduct(arabicName: "الاسم الأصلي");

        Should.Throw<BusinessRuleException>(() => UpdateWith(product, arabicName: string.Empty));

        // The invariant gate runs before any scalar is applied, so a rejected edit
        // leaves the aggregate untouched.
        product.ArabicName.ShouldBe("الاسم الأصلي");
        product.Units.Count.ShouldBe(4);
    }
}
