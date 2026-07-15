using VetFlow.Domain.Catalog;

namespace VetFlow.Domain.Tests.Catalog;

/// <summary>Builders for valid aggregates, mirroring TS-CAT-012's carton→box→strip→pill chain.</summary>
public static class ProductTestData
{
    public static readonly Guid CartonUnitId = Guid.NewGuid();
    public static readonly Guid BoxUnitId = Guid.NewGuid();
    public static readonly Guid StripUnitId = Guid.NewGuid();
    public static readonly Guid PillUnitId = Guid.NewGuid();

    public static ProductCapabilities DefaultCapabilities() => new(
        isSplittable: true,
        isRefrigerated: false,
        hasExpiration: true,
        hasOpenExpiration: false,
        openExpirationPeriod: null);

    public static List<ProductUnit> MultiLevelChain() =>
    [
        new(Guid.NewGuid(), CartonUnitId, position: 0, quantityInNextUnit: 12, isPurchaseUnit: true, isSaleUnit: false),
        new(Guid.NewGuid(), BoxUnitId, position: 1, quantityInNextUnit: 10, isPurchaseUnit: false, isSaleUnit: true, sellingPrice: 120m),
        new(Guid.NewGuid(), StripUnitId, position: 2, quantityInNextUnit: 10, isPurchaseUnit: false, isSaleUnit: true, barcode: "6221000000012", sellingPrice: 15m),
        new(Guid.NewGuid(), PillUnitId, position: 3, quantityInNextUnit: null, isPurchaseUnit: false, isSaleUnit: true, sellingPrice: 2m),
    ];

    public static Product ValidProduct(
        string arabicName = "أموكسيسيلين 500",
        IReadOnlyCollection<ProductUnit>? units = null,
        Guid? storageUnitId = null,
        Guid? defaultSaleUnitId = null,
        Guid? defaultPurchaseUnitId = null,
        Guid? categoryId = null,
        Guid? manufacturerId = null,
        Guid? natureId = null,
        ProductCapabilities? capabilities = null) => new(
        Guid.NewGuid(),
        arabicName,
        categoryId ?? Guid.NewGuid(),
        manufacturerId ?? Guid.NewGuid(),
        natureId ?? Guid.NewGuid(),
        capabilities ?? DefaultCapabilities(),
        units ?? MultiLevelChain(),
        storageUnitId ?? PillUnitId,
        defaultSaleUnitId ?? StripUnitId,
        defaultPurchaseUnitId ?? CartonUnitId);
}
