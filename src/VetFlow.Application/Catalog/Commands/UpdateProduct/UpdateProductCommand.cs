using VetFlow.Application.Catalog.Commands.Common;
using VetFlow.Application.Common;

namespace VetFlow.Application.Catalog.Commands.UpdateProduct;

/// <summary>
/// Update a product's non-audited basic data (screen S3 edit mode; REQ-CAT-038
/// first clause / BR-CAT-041, REQ-CAT-003): identity, classification, capabilities,
/// notes, and the unit profile (DEC-CAT-007/020). Selling-price editing and the
/// dangerous unit-profile confirmation+audit path are deferred (DEC-CAT-031), so no
/// price is carried here. The result is the product id, or <c>null</c> when no
/// product with <see cref="Id"/> exists (→ 404) — mirroring the read side
/// (STD-BE-021: an id, never a read DTO).
/// </summary>
public sealed record UpdateProductCommand : ICommand<Guid?>, IProductWriteCommand
{
    public required Guid Id { get; init; }

    public required string ArabicName { get; init; }

    public string? EnglishName { get; init; }

    public string? Size { get; init; }

    public string? Concentration { get; init; }

    public required Guid CategoryId { get; init; }

    public required Guid ManufacturerId { get; init; }

    public required Guid NatureId { get; init; }

    public bool IsSplittable { get; init; }

    public bool IsRefrigerated { get; init; }

    public bool HasExpiration { get; init; }

    public bool HasOpenExpiration { get; init; }

    /// <summary>Mandatory only when <see cref="HasOpenExpiration"/> (BR-CAT-036, AC-CAT-031).</summary>
    public int? OpenExpirationPeriodDays { get; init; }

    public string? InternalNotes { get; init; }

    public required IReadOnlyList<UpdateProductUnitInput> Units { get; init; }

    public required Guid StorageUnitId { get; init; }

    public required Guid DefaultSaleUnitId { get; init; }

    public required Guid DefaultPurchaseUnitId { get; init; }

    IReadOnlyList<IProductUnitWriteInput> IProductWriteCommand.Units => Units;
}

/// <summary>One unit-profile row in an update request — no selling price (DEC-CAT-031).</summary>
public sealed record UpdateProductUnitInput : IProductUnitWriteInput
{
    public required Guid UnitId { get; init; }

    public required int Position { get; init; }

    /// <summary>User-entered conversion factor to the next unit; null on the smallest unit (BR-CAT-018).</summary>
    public decimal? QuantityInNextUnit { get; init; }

    public bool IsPurchaseUnit { get; init; }

    public bool IsSaleUnit { get; init; }

    public string? Barcode { get; init; }
}
