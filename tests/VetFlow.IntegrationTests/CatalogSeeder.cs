using VetFlow.Domain.Catalog;
using VetFlow.Domain.Categories;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.IntegrationTests;

/// <summary>Builds valid aggregates through the domain constructors — never raw rows.</summary>
public static class CatalogSeeder
{
    public static Category NewCategory(VetFlowDbContext dbContext, string name)
    {
        var category = new Category(Guid.NewGuid(), name);
        dbContext.Categories.Add(category);
        return category;
    }

    public static Manufacturer NewManufacturer(VetFlowDbContext dbContext, string name)
    {
        var manufacturer = new Manufacturer(Guid.NewGuid(), name);
        dbContext.Manufacturers.Add(manufacturer);
        return manufacturer;
    }

    public static Product NewProduct(
        VetFlowDbContext dbContext,
        string arabicName,
        Guid categoryId,
        Guid manufacturerId,
        Guid natureId,
        string? englishName = null,
        string? size = null,
        string? concentration = null,
        bool isSplittable = false,
        bool isRefrigerated = false,
        bool hasExpiration = false,
        decimal? boxPrice = null,
        string? boxBarcode = null,
        string? stripBarcode = null,
        decimal? stripPrice = null)
    {
        var units = new List<ProductUnit>
        {
            new(Guid.NewGuid(), SeededCatalogIds.CartonUnit, position: 0, quantityInNextUnit: 12,
                isPurchaseUnit: true, isSaleUnit: false),
            new(Guid.NewGuid(), SeededCatalogIds.BoxUnit, position: 1, quantityInNextUnit: 10,
                isPurchaseUnit: false, isSaleUnit: true, barcode: boxBarcode, sellingPrice: boxPrice),
            new(Guid.NewGuid(), SeededCatalogIds.StripUnit, position: 2, quantityInNextUnit: null,
                isPurchaseUnit: false, isSaleUnit: true, barcode: stripBarcode, sellingPrice: stripPrice),
        };

        var product = new Product(
            Guid.NewGuid(),
            // Synthetic, non-numeric code: never collides with the PRD-000001 sequence
            // codes that the create command generates (advisor guard).
            $"PRD-SEED-{Guid.NewGuid():N}",
            arabicName,
            categoryId,
            manufacturerId,
            natureId,
            new ProductCapabilities(isSplittable, isRefrigerated, hasExpiration, false, null),
            units,
            storageUnitId: SeededCatalogIds.StripUnit,
            defaultSaleUnitId: SeededCatalogIds.BoxUnit,
            defaultPurchaseUnitId: SeededCatalogIds.CartonUnit,
            englishName: englishName,
            size: size,
            concentration: concentration);

        dbContext.Products.Add(product);
        return product;
    }

    /// <summary>
    /// Represents pre-existing disabled state (BR-CAT-038). The deactivation
    /// workflow itself (WF-CAT-008, blocker checks BR-CAT-039) is a later slice,
    /// so no domain transition exists yet — tests write the persisted state.
    /// </summary>
    public static void MarkDisabled(VetFlowDbContext dbContext, Product product) =>
        dbContext.Entry(product).Property(entity => entity.Status).CurrentValue = ProductStatus.Disabled;
}
