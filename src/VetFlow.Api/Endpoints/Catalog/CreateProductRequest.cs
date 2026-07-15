using VetFlow.Application.Catalog.Commands.CreateProduct;

namespace VetFlow.Api.Endpoints.Catalog;

/// <summary>
/// JSON body of POST /api/v1/products (camelCase, STD-API-032). A pure DTO —
/// the endpoint exposes no domain entity (STD-API-035). Missing values map to
/// their empty form so the command validator produces the canonical per-field
/// validation shape (STD-API-014).
/// </summary>
public sealed record CreateProductRequest
{
    public string? ArabicName { get; init; }

    public string? EnglishName { get; init; }

    public string? Size { get; init; }

    public string? Concentration { get; init; }

    public Guid CategoryId { get; init; }

    public Guid ManufacturerId { get; init; }

    public Guid NatureId { get; init; }

    public bool IsSplittable { get; init; }

    public bool IsRefrigerated { get; init; }

    public bool HasExpiration { get; init; }

    public bool HasOpenExpiration { get; init; }

    public int? OpenExpirationPeriodDays { get; init; }

    public string? InternalNotes { get; init; }

    public IReadOnlyList<CreateProductUnitRequest>? Units { get; init; }

    public Guid StorageUnitId { get; init; }

    public Guid DefaultSaleUnitId { get; init; }

    public Guid DefaultPurchaseUnitId { get; init; }

    public CreateProductCommand ToCommand() => new()
    {
        ArabicName = ArabicName ?? string.Empty,
        EnglishName = EnglishName,
        Size = Size,
        Concentration = Concentration,
        CategoryId = CategoryId,
        ManufacturerId = ManufacturerId,
        NatureId = NatureId,
        IsSplittable = IsSplittable,
        IsRefrigerated = IsRefrigerated,
        HasExpiration = HasExpiration,
        HasOpenExpiration = HasOpenExpiration,
        OpenExpirationPeriodDays = OpenExpirationPeriodDays,
        InternalNotes = InternalNotes,
        Units = (Units ?? [])
            .Select((unit, index) => new CreateProductUnitInput
            {
                UnitId = unit.UnitId,
                Position = unit.Position ?? index,
                QuantityInNextUnit = unit.QuantityInNextUnit,
                IsPurchaseUnit = unit.IsPurchaseUnit,
                IsSaleUnit = unit.IsSaleUnit,
                Barcode = unit.Barcode,
                SellingPrice = unit.SellingPrice,
            })
            .ToList(),
        StorageUnitId = StorageUnitId,
        DefaultSaleUnitId = DefaultSaleUnitId,
        DefaultPurchaseUnitId = DefaultPurchaseUnitId,
    };
}

/// <summary>One unit-profile row in a create request body.</summary>
public sealed record CreateProductUnitRequest
{
    public Guid UnitId { get; init; }

    public int? Position { get; init; }

    public decimal? QuantityInNextUnit { get; init; }

    public bool IsPurchaseUnit { get; init; }

    public bool IsSaleUnit { get; init; }

    public string? Barcode { get; init; }

    public decimal? SellingPrice { get; init; }
}
