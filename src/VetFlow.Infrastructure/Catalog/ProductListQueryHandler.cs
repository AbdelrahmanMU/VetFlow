using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.ProductList;
using VetFlow.Application.Common;
using VetFlow.Domain.Catalog;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// Product list query implementation (screen S1). Projects straight to the
/// response DTO — deliberate CQRS-lite read that bypasses the domain
/// (ADR-0014 §5). The category name join is a sanctioned cross-module read
/// inside a query handler (ADR-0014 §2).
/// </summary>
public sealed class ProductListQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<ProductListQuery, PagedResult<ProductListItemDto>>
{
    private const string LikeEscapeCharacter = "\\";

    public async Task<PagedResult<ProductListItemDto>> HandleAsync(
        ProductListQuery query,
        CancellationToken cancellationToken)
    {
        var products = ApplyFilters(dbContext.Products.AsNoTracking(), query);

        var rows =
            from product in products
            join category in dbContext.Categories.AsNoTracking() on product.CategoryId equals category.Id
            join manufacturer in dbContext.Manufacturers.AsNoTracking() on product.ManufacturerId equals manufacturer.Id
            join nature in dbContext.ProductNatures.AsNoTracking() on product.NatureId equals nature.Id
            join storageUnit in dbContext.Units.AsNoTracking() on product.StorageUnitId equals storageUnit.Id
            join defaultSaleUnit in dbContext.Units.AsNoTracking() on product.DefaultSaleUnitId equals defaultSaleUnit.Id
            select new ProductListRow
            {
                Id = product.Id,
                ArabicName = product.ArabicName,
                EnglishName = product.EnglishName,
                Size = product.Size,
                Concentration = product.Concentration,
                CategoryName = category.Name,
                ManufacturerName = manufacturer.Name,
                NatureName = nature.Name,
                StorageUnitName = storageUnit.Name,
                DefaultSaleUnitName = defaultSaleUnit.Name,
                DefaultSaleUnitPrice = product.Units
                    .Where(unit => unit.UnitId == product.DefaultSaleUnitId)
                    .Select(unit => unit.SellingPrice)
                    .FirstOrDefault(),
                Status = product.Status,
                IsSplittable = product.IsSplittable,
                IsRefrigerated = product.IsRefrigerated,
                HasExpiration = product.HasExpiration,
                HasSellingPrice = product.Units.Any(unit => unit.IsSaleUnit && unit.SellingPrice != null),
            };

        var totalCount = await rows.CountAsync(cancellationToken);

        var items = await ApplySorting(rows, query)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductListItemDto>
        {
            Items = items.Select(ToDto).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    private static IQueryable<Product> ApplyFilters(IQueryable<Product> products, ProductListQuery query)
    {
        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(product => product.CategoryId == categoryId);
        }

        if (query.ManufacturerId is { } manufacturerId)
        {
            products = products.Where(product => product.ManufacturerId == manufacturerId);
        }

        if (query.NatureId is { } natureId)
        {
            products = products.Where(product => product.NatureId == natureId);
        }

        if (query.Status is { } status)
        {
            var productStatus = status == ProductStatusFilter.Active ? ProductStatus.Active : ProductStatus.Disabled;
            products = products.Where(product => product.Status == productStatus);
        }

        if (query.IsRefrigerated is { } isRefrigerated)
        {
            products = products.Where(product => product.IsRefrigerated == isRefrigerated);
        }

        if (query.HasExpiration is { } hasExpiration)
        {
            products = products.Where(product => product.HasExpiration == hasExpiration);
        }

        if (query.IsSplittable is { } isSplittable)
        {
            products = products.Where(product => product.IsSplittable == isSplittable);
        }

        if (query.HasSellingPrice is { } hasSellingPrice)
        {
            products = products.Where(product =>
                product.Units.Any(unit => unit.IsSaleUnit && unit.SellingPrice != null) == hasSellingPrice);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var rawSearch = query.Search.Trim();
            var pattern = $"%{EscapeLike(ArabicSearchText.Normalize(rawSearch))}%";
            products = products.Where(product =>
                EF.Functions.ILike(EF.Property<string>(product, SearchableText.PropertyName), pattern, LikeEscapeCharacter)
                || product.Units.Any(unit => unit.Barcode == rawSearch));
        }

        return products;
    }

    private static IOrderedQueryable<ProductListRow> ApplySorting(
        IQueryable<ProductListRow> rows,
        ProductListQuery query)
    {
        var ascending = query.Direction == SortDirection.Ascending;

        var ordered = query.Sort switch
        {
            ProductListSortField.Category => ascending
                ? rows.OrderBy(row => row.CategoryName)
                : rows.OrderByDescending(row => row.CategoryName),
            ProductListSortField.Manufacturer => ascending
                ? rows.OrderBy(row => row.ManufacturerName)
                : rows.OrderByDescending(row => row.ManufacturerName),
            ProductListSortField.Nature => ascending
                ? rows.OrderBy(row => row.NatureName)
                : rows.OrderByDescending(row => row.NatureName),
            ProductListSortField.Status => ascending
                ? rows.OrderBy(row => row.Status)
                : rows.OrderByDescending(row => row.Status),
            ProductListSortField.Price => ascending
                ? rows.OrderBy(row => row.DefaultSaleUnitPrice == null).ThenBy(row => row.DefaultSaleUnitPrice)
                : rows.OrderBy(row => row.DefaultSaleUnitPrice == null).ThenByDescending(row => row.DefaultSaleUnitPrice),
            _ => ascending
                ? rows.OrderBy(row => row.ArabicName)
                : rows.OrderByDescending(row => row.ArabicName),
        };

        var withNameKey = query.Sort == ProductListSortField.Name
            ? ordered
            : ordered.ThenBy(row => row.ArabicName);

        // A unique final key gives offset pagination a total order, so pages stay
        // stable even when Arabic names collide (duplicates are allowed —
        // BR-CAT-042 / DEC-CAT-018); without it, tied rows can be skipped or repeated.
        return withNameKey.ThenBy(row => row.Id);
    }

    private static ProductListItemDto ToDto(ProductListRow row) => new()
    {
        Id = row.Id,
        ArabicName = row.ArabicName,
        EnglishName = row.EnglishName,
        Size = row.Size,
        Concentration = row.Concentration,
        CategoryName = row.CategoryName,
        ManufacturerName = row.ManufacturerName,
        NatureName = row.NatureName,
        StorageUnitName = row.StorageUnitName,
        DefaultSaleUnitName = row.DefaultSaleUnitName,
        DefaultSaleUnitPrice = row.DefaultSaleUnitPrice is { } price
            ? new MoneyDto { Amount = price, Currency = Currencies.EgyptianPound }
            : null,
        Status = row.Status == ProductStatus.Active ? ProductStatusDto.Active : ProductStatusDto.Disabled,
        IsSplittable = row.IsSplittable,
        IsRefrigerated = row.IsRefrigerated,
        HasExpiration = row.HasExpiration,
        HasSellingPrice = row.HasSellingPrice,
    };

    private static string EscapeLike(string value) =>
        value.Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);

    private sealed record ProductListRow
    {
        public required Guid Id { get; init; }

        public required string ArabicName { get; init; }

        public string? EnglishName { get; init; }

        public string? Size { get; init; }

        public string? Concentration { get; init; }

        public required string CategoryName { get; init; }

        public required string ManufacturerName { get; init; }

        public required string NatureName { get; init; }

        public required string StorageUnitName { get; init; }

        public required string DefaultSaleUnitName { get; init; }

        public decimal? DefaultSaleUnitPrice { get; init; }

        public required ProductStatus Status { get; init; }

        public required bool IsSplittable { get; init; }

        public required bool IsRefrigerated { get; init; }

        public required bool HasExpiration { get; init; }

        public required bool HasSellingPrice { get; init; }
    }
}
