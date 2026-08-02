using VetFlow.Domain.Catalog;
using VetFlow.Domain.Common;
using VetFlow.Domain.Identity;
using VetFlow.Domain.Inventory;
using VetFlow.Domain.Purchasing;
using VetFlow.Domain.Sales;

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
            new()
            {
                Code = PurchasingErrorCodes.InvoiceNotDraft,
                Status = StatusCodes.Status409Conflict,
                Title = "Only a draft purchase invoice may change",
            },
            new()
            {
                Code = PurchasingErrorCodes.LineComposition,
                Status = StatusCodes.Status400BadRequest,
                Title = "The purchase line is invalid",
            },
            new()
            {
                Code = PurchasingErrorCodes.InvoiceHasNoLines,
                Status = StatusCodes.Status409Conflict,
                Title = "A purchase invoice with no lines cannot be received",
            },
            new()
            {
                Code = PurchasingErrorCodes.ExpiryRequired,
                Status = StatusCodes.Status400BadRequest,
                Title = "This product requires an expiry date at receiving",
            },
            new()
            {
                Code = PurchasingErrorCodes.ReturnOriginalInvoiceNotReceived,
                Status = StatusCodes.Status409Conflict,
                Title = "A purchase return requires a received purchase invoice",
            },
            new()
            {
                Code = PurchasingErrorCodes.ReturnQuantityExceedsReturnable,
                Status = StatusCodes.Status409Conflict,
                Title = "Returned quantity exceeds what remains returnable on this line",
            },
            new()
            {
                // 400, not 409: a non-positive quantity is a malformed request, not a state
                // conflict — the LineComposition precedent.
                Code = PurchasingErrorCodes.ReturnLineComposition,
                Status = StatusCodes.Status400BadRequest,
                Title = "Purchase return line composition is invalid",
            },
            new()
            {
                Code = PurchasingErrorCodes.ReturnNotDraft,
                Status = StatusCodes.Status409Conflict,
                Title = "Only a draft purchase return may change or be committed",
            },
            new()
            {
                Code = PurchasingErrorCodes.ReturnHasNoLines,
                Status = StatusCodes.Status409Conflict,
                Title = "A purchase return with no lines cannot be committed",
            },
            new()
            {
                Code = SalesErrorCodes.InvoiceNotDraft,
                Status = StatusCodes.Status409Conflict,
                Title = "Only a draft sales invoice may change or be committed",
            },
            new()
            {
                Code = SalesErrorCodes.LineComposition,
                Status = StatusCodes.Status400BadRequest,
                Title = "The sales line is invalid",
            },
            new()
            {
                Code = SalesErrorCodes.InvoiceHasNoLines,
                Status = StatusCodes.Status409Conflict,
                Title = "A sales invoice with no lines cannot be committed",
            },
            new()
            {
                Code = SalesErrorCodes.InexactUnitConversion,
                Status = StatusCodes.Status400BadRequest,
                Title = "The quantity does not convert into the stock unit exactly",
            },
            new()
            {
                Code = SalesErrorCodes.ReturnOriginalInvoiceNotCommitted,
                Status = StatusCodes.Status409Conflict,
                Title = "A sales return requires a committed sales invoice",
            },
            new()
            {
                Code = SalesErrorCodes.ReturnQuantityExceedsReturnable,
                Status = StatusCodes.Status409Conflict,
                Title = "Returned quantity exceeds what remains returnable on this line",
            },
            new()
            {
                // 400, not 409: a malformed quantity is a bad request, not a state conflict — the
                // LineComposition precedent.
                Code = SalesErrorCodes.ReturnLineComposition,
                Status = StatusCodes.Status400BadRequest,
                Title = "Sales return line composition is invalid",
            },
            new()
            {
                Code = SalesErrorCodes.ReturnNotDraft,
                Status = StatusCodes.Status409Conflict,
                Title = "Only a draft sales return may change or be committed",
            },
            new()
            {
                Code = SalesErrorCodes.ReturnHasNoLines,
                Status = StatusCodes.Status409Conflict,
                Title = "A sales return with no lines cannot be committed",
            },
            new()
            {
                // 409: the document and the recorded consumption disagree, so the return cannot be
                // placed. Not a field the user can correct, but a state that blocks the operation.
                Code = SalesErrorCodes.ReturnConsumptionTraceUnusable,
                Status = StatusCodes.Status409Conflict,
                Title = "The sale's recorded consumption cannot support this return",
            },
            new()
            {
                Code = InventoryErrorCodes.ConsumptionRequestInvalid,
                Status = StatusCodes.Status400BadRequest,
                Title = "The inventory consumption request is invalid",
            },
            new()
            {
                Code = InventoryErrorCodes.InsufficientStock,
                Status = StatusCodes.Status409Conflict,
                Title = "Saleable stock is not sufficient",
            },
            new()
            {
                Code = InventoryErrorCodes.ConcurrencyConflict,
                Status = StatusCodes.Status409Conflict,
                Title = "Inventory changed while the sale was being committed; retry",
            },
            new()
            {
                Code = InventoryErrorCodes.QuantityBelowZero,
                Status = StatusCodes.Status409Conflict,
                Title = "The operation would drive the batch quantity below zero",
            },
            new()
            {
                Code = InventoryErrorCodes.ReasonNotAllowed,
                Status = StatusCodes.Status400BadRequest,
                Title = "The reason code does not belong to this operation",
            },
            new()
            {
                Code = InventoryErrorCodes.OperationConcurrencyConflict,
                Status = StatusCodes.Status409Conflict,
                Title = "The batch changed while the operation was being written; retry",
            },
            new()
            {
                // One entry for every sign-in failure. The title is deliberately as
                // uninformative as the message: an unknown phone number and a wrong password
                // must be indistinguishable (BR-IDN-003).
                Code = IdentityErrorCodes.SignInFailed,
                Status = StatusCodes.Status401Unauthorized,
                Title = "Sign-in failed",
            },
            new()
            {
                Code = IdentityErrorCodes.NotAuthenticated,
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication is required",
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
