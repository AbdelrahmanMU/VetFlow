using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.ProductDetails;
using VetFlow.Application.Catalog.Queries.ProductList;
using VetFlow.Application.Common;
using VetFlow.Domain.Catalog;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// Product-details read (screen S2). Projects straight to the response DTO — a
/// deliberate CQRS-lite read that bypasses the domain (ADR-0014 §5). The unit
/// profile carries its distinguished units (storage + defaults) so the page can
/// mark them (REQ-CAT-015). Returns <c>null</c> when the product does not exist.
/// </summary>
public sealed class ProductDetailsQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<ProductDetailsQuery, ProductDetailsDto?>
{
    public async Task<ProductDetailsDto?> HandleAsync(
        ProductDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var row = await (
            from product in dbContext.Products.AsNoTracking()
            where product.Id == query.Id
            join category in dbContext.Categories.AsNoTracking() on product.CategoryId equals category.Id
            join manufacturer in dbContext.Manufacturers.AsNoTracking() on product.ManufacturerId equals manufacturer.Id
            join nature in dbContext.ProductNatures.AsNoTracking() on product.NatureId equals nature.Id
            select new ProductDetailsRow
            {
                Id = product.Id,
                InternalCode = product.InternalCode,
                ArabicName = product.ArabicName,
                EnglishName = product.EnglishName,
                Size = product.Size,
                Concentration = product.Concentration,
                CategoryId = product.CategoryId,
                ManufacturerId = product.ManufacturerId,
                NatureId = product.NatureId,
                CategoryName = category.Name,
                ManufacturerName = manufacturer.Name,
                NatureName = nature.Name,
                Status = product.Status,
                IsSplittable = product.IsSplittable,
                IsRefrigerated = product.IsRefrigerated,
                HasExpiration = product.HasExpiration,
                HasOpenExpiration = product.HasOpenExpiration,
                OpenExpirationPeriod = product.OpenExpirationPeriod,
                InternalNotes = product.InternalNotes,
                StorageUnitId = product.StorageUnitId,
                DefaultSaleUnitId = product.DefaultSaleUnitId,
                DefaultPurchaseUnitId = product.DefaultPurchaseUnitId,
                HasSellingPrice = product.Units.Any(unit => unit.IsSaleUnit && unit.SellingPrice != null),
                Units = product.Units
                    .Select(unit => new ProductUnitRow
                    {
                        UnitId = unit.UnitId,
                        UnitName = dbContext.Units
                            .Where(candidate => candidate.Id == unit.UnitId)
                            .Select(candidate => candidate.Name)
                            .FirstOrDefault(),
                        Position = unit.Position,
                        QuantityInNextUnit = unit.QuantityInNextUnit,
                        IsPurchaseUnit = unit.IsPurchaseUnit,
                        IsSaleUnit = unit.IsSaleUnit,
                        Barcode = unit.Barcode,
                        SellingPrice = unit.SellingPrice,
                    })
                    .ToList(),
            }).FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToDto(row);
    }

    private static ProductDetailsDto ToDto(ProductDetailsRow row) => new()
    {
        Id = row.Id,
        InternalCode = row.InternalCode,
        ArabicName = row.ArabicName,
        EnglishName = row.EnglishName,
        Size = row.Size,
        Concentration = row.Concentration,
        CategoryId = row.CategoryId,
        ManufacturerId = row.ManufacturerId,
        NatureId = row.NatureId,
        CategoryName = row.CategoryName,
        ManufacturerName = row.ManufacturerName,
        NatureName = row.NatureName,
        Status = row.Status == ProductStatus.Active ? ProductStatusDto.Active : ProductStatusDto.Disabled,
        IsSplittable = row.IsSplittable,
        IsRefrigerated = row.IsRefrigerated,
        HasExpiration = row.HasExpiration,
        HasOpenExpiration = row.HasOpenExpiration,
        OpenExpirationPeriodDays = row.OpenExpirationPeriod is { } period ? (int)Math.Round(period.TotalDays) : null,
        InternalNotes = row.InternalNotes,
        HasSellingPrice = row.HasSellingPrice,
        Units = row.Units
            .OrderBy(unit => unit.Position)
            .Select(unit => new ProductUnitDetailsDto
            {
                UnitId = unit.UnitId,
                UnitName = unit.UnitName ?? string.Empty,
                Position = unit.Position,
                QuantityInNextUnit = unit.QuantityInNextUnit,
                IsPurchaseUnit = unit.IsPurchaseUnit,
                IsSaleUnit = unit.IsSaleUnit,
                Barcode = unit.Barcode,
                SellingPrice = unit.SellingPrice is { } price
                    ? new MoneyDto { Amount = price, Currency = Currencies.EgyptianPound }
                    : null,
                IsStorageUnit = unit.UnitId == row.StorageUnitId,
                IsDefaultSaleUnit = unit.UnitId == row.DefaultSaleUnitId,
                IsDefaultPurchaseUnit = unit.UnitId == row.DefaultPurchaseUnitId,
            })
            .ToList(),
    };

    private sealed record ProductDetailsRow
    {
        public required Guid Id { get; init; }

        public required string InternalCode { get; init; }

        public required string ArabicName { get; init; }

        public string? EnglishName { get; init; }

        public string? Size { get; init; }

        public string? Concentration { get; init; }

        public required Guid CategoryId { get; init; }

        public required Guid ManufacturerId { get; init; }

        public required Guid NatureId { get; init; }

        public required string CategoryName { get; init; }

        public required string ManufacturerName { get; init; }

        public required string NatureName { get; init; }

        public required ProductStatus Status { get; init; }

        public required bool IsSplittable { get; init; }

        public required bool IsRefrigerated { get; init; }

        public required bool HasExpiration { get; init; }

        public required bool HasOpenExpiration { get; init; }

        public TimeSpan? OpenExpirationPeriod { get; init; }

        public string? InternalNotes { get; init; }

        public required Guid StorageUnitId { get; init; }

        public required Guid DefaultSaleUnitId { get; init; }

        public required Guid DefaultPurchaseUnitId { get; init; }

        public required bool HasSellingPrice { get; init; }

        public required List<ProductUnitRow> Units { get; init; }
    }

    private sealed record ProductUnitRow
    {
        public required Guid UnitId { get; init; }

        public string? UnitName { get; init; }

        public required int Position { get; init; }

        public decimal? QuantityInNextUnit { get; init; }

        public required bool IsPurchaseUnit { get; init; }

        public required bool IsSaleUnit { get; init; }

        public string? Barcode { get; init; }

        public decimal? SellingPrice { get; init; }
    }
}
