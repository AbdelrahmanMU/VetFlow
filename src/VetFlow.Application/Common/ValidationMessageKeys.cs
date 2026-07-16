namespace VetFlow.Application.Common;

/// <summary>
/// Message resource keys carried by validation failures. Validators and parsers
/// emit keys only; the API middleware is the single translation point
/// (STD-API-012, ADR-0018).
/// </summary>
public static class ValidationMessageKeys
{
    public const string PageMin = "validation.page.min";
    public const string PageMax = "validation.page.max";
    public const string PageSizeRange = "validation.pageSize.range";
    public const string SearchMaxLength = "validation.search.maxLength";
    public const string InvalidId = "validation.id.invalid";
    public const string InvalidBoolean = "validation.boolean.invalid";
    public const string InvalidInteger = "validation.integer.invalid";
    public const string UnknownSortField = "validation.sort.unknown";
    public const string UnknownSortDirection = "validation.dir.unknown";
    public const string UnknownStatus = "validation.status.unknown";

    // Create product (WF-CAT-001 / REQ-CAT-043 / BR-CAT-009): one key per missing
    // minimum field, so the response names exactly which field is missing (AC-CAT-043).
    public const string ArabicNameRequired = "validation.arabicName.required";
    public const string CategoryRequired = "validation.category.required";
    public const string ManufacturerRequired = "validation.manufacturer.required";
    public const string NatureRequired = "validation.nature.required";
    public const string UnitProfileRequired = "validation.units.required";
    public const string PurchaseUnitRequired = "validation.units.purchaseRequired";
    public const string SaleUnitRequired = "validation.units.saleRequired";
    public const string StorageUnitRequired = "validation.storageUnit.required";
    public const string DefaultSaleUnitRequired = "validation.saleUnit.defaultRequired";
    public const string DefaultPurchaseUnitRequired = "validation.purchaseUnit.defaultRequired";
    public const string OpenExpirationPeriodRequired = "validation.openExpiration.periodRequired";
    public const string ConversionFactorPositive = "validation.conversionFactor.positive";
    public const string TextTooLong = "validation.text.tooLong";

    // Possible-duplicate advisory read (DEC-CAT-027 / REQ-CAT-042).
    public const string DuplicateArabicNameRequired = "validation.duplicateCheck.arabicName.required";
    public const string DuplicateManufacturerRequired = "validation.duplicateCheck.manufacturer.required";

    // Managed data — categories (REQ-CTG-002/003, BR-CTG-002/003). The duplicate
    // key is raised by the handler (uniqueness needs the database), not a validator.
    public const string CategoryNameRequired = "validation.category.name.required";
    public const string CategoryNameDuplicate = "validation.category.name.duplicate";

    // Managed data — manufacturers (REQ-CAT-013/047/048, BR-CAT-007/052). A deliberate
    // copy of the category keys (Catalog owns manufacturers). The duplicate key is
    // raised by the handler (uniqueness needs the database), not a validator.
    public const string ManufacturerNameRequired = "validation.manufacturer.name.required";
    public const string ManufacturerNameDuplicate = "validation.manufacturer.name.duplicate";
}
