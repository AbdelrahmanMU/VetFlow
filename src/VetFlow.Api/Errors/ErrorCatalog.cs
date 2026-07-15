using VetFlow.Domain.Catalog;
using VetFlow.Domain.Common;

namespace VetFlow.Api.Errors;

/// <summary>
/// The Error Catalog (ADR-0018 §4): business rule → error code → HTTP status →
/// localized message. One code, one place; duplicate codes fail construction.
/// </summary>
public static class ErrorCatalog
{
    public const int DefaultBusinessRuleStatus = StatusCodes.Status409Conflict;

    private static readonly IReadOnlyDictionary<string, ErrorCatalogEntry> Entries =
        new ErrorCatalogEntry[]
        {
            new()
            {
                Code = CommonErrorCodes.Validation,
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred",
            },
            new()
            {
                Code = CatalogErrorCodes.MinimumProductData,
                Status = StatusCodes.Status400BadRequest,
                Title = "Product does not meet the mandatory creation minimum",
            },
            new()
            {
                Code = CatalogErrorCodes.UnitProfileComposition,
                Status = StatusCodes.Status400BadRequest,
                Title = "Unit profile composition is invalid",
            },
            new()
            {
                Code = CatalogErrorCodes.StorageUnitNotInProfile,
                Status = StatusCodes.Status400BadRequest,
                Title = "Stock-keeping unit must be one of the profile units",
            },
            new()
            {
                Code = CatalogErrorCodes.DefaultPurchaseUnitInvalid,
                Status = StatusCodes.Status400BadRequest,
                Title = "Default purchase unit must be a purchase unit from the profile",
            },
            new()
            {
                Code = CatalogErrorCodes.DefaultSaleUnitInvalid,
                Status = StatusCodes.Status400BadRequest,
                Title = "Default sale unit must be a sale unit from the profile",
            },
            new()
            {
                Code = CatalogErrorCodes.PriceOnNonSaleUnit,
                Status = StatusCodes.Status400BadRequest,
                Title = "A selling price is allowed only on sale units",
            },
            new()
            {
                Code = CatalogErrorCodes.OpenExpirationPeriodRequired,
                Status = StatusCodes.Status400BadRequest,
                Title = "Open-expiration products require the open-expiration period",
            },
        }.ToDictionary(entry => entry.Code);

    public static ErrorCatalogEntry Get(string code) =>
        Entries.TryGetValue(code, out var entry)
            ? entry
            : new ErrorCatalogEntry
            {
                Code = code,
                Status = DefaultBusinessRuleStatus,
                Title = "Business rule violated",
            };

    public static IReadOnlyCollection<ErrorCatalogEntry> All => [.. Entries.Values];
}
