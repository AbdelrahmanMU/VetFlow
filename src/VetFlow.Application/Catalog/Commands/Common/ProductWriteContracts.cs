namespace VetFlow.Application.Catalog.Commands.Common;

/// <summary>
/// The fields shared by every product write command — create and update. Defining
/// them once lets the shared validator (<see cref="ProductWriteCommandValidator{TCommand}"/>)
/// and the endpoint contracts stay in sync without duplication (DEC-CAT-031). The
/// unit-row abstraction deliberately excludes the selling price: prices are set on
/// create and edited only on the separately audited price path, so update carries
/// no price field.
/// </summary>
public interface IProductWriteCommand
{
    string ArabicName { get; }

    string? EnglishName { get; }

    string? Size { get; }

    string? Concentration { get; }

    Guid CategoryId { get; }

    Guid ManufacturerId { get; }

    Guid NatureId { get; }

    bool IsSplittable { get; }

    bool IsRefrigerated { get; }

    bool HasExpiration { get; }

    bool HasOpenExpiration { get; }

    int? OpenExpirationPeriodDays { get; }

    string? InternalNotes { get; }

    IReadOnlyList<IProductUnitWriteInput> Units { get; }

    Guid StorageUnitId { get; }

    Guid DefaultSaleUnitId { get; }

    Guid DefaultPurchaseUnitId { get; }
}

/// <summary>One unit-profile row shared by create and update (BR-CAT-016/018/024).</summary>
public interface IProductUnitWriteInput
{
    Guid UnitId { get; }

    int Position { get; }

    decimal? QuantityInNextUnit { get; }

    bool IsPurchaseUnit { get; }

    bool IsSaleUnit { get; }

    string? Barcode { get; }
}
