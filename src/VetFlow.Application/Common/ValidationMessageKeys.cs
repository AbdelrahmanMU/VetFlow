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
    public const string InvalidDate = "validation.date.invalid";
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

    // Create purchase invoice (REQ-PUR-003 / AC-PUR-007 / BR-PUR-001): the required
    // header fields each raise their own field-keyed error so the response names
    // exactly which field is missing.
    public const string SupplierNameRequired = "validation.supplierName.required";
    public const string InvoiceDateRequired = "validation.invoiceDate.required";

    // Add purchase line item (REQ-PUR-004 / AC-PUR-009 / BR-PUR-005): each field of a
    // line raises its own field-keyed error so the add-line dialog highlights exactly
    // what is wrong.
    public const string LineProductRequired = "validation.line.product.required";
    public const string LinePurchaseUnitRequired = "validation.line.purchaseUnit.required";
    public const string LineQuantityPositive = "validation.line.quantity.positive";
    public const string LineUnitPriceNonNegative = "validation.line.unitPrice.nonNegative";

    // Sales (REQ-SAL-001 / AC-SAL-002 / AC-SAL-003 / BR-SAL-001 / BR-SAL-004). The sale date is
    // the only required header field — the customer name is optional (DEC-SAL-002). The sale unit
    // gets its own key: it is a different constraint from the purchase unit (BR-SAL-004).
    public const string SaleDateRequired = "validation.saleDate.required";
    public const string LineSaleUnitRequired = "validation.line.saleUnit.required";

    // Inventory adjustments (REQ-INV-010 / AC-INV-051..054 / BR-INV-061/066/067). The batch, the
    // direction, a positive magnitude and a reason are each required in their own right, so a
    // rejection names the field the form must highlight. The reason's *membership* of the
    // adjustment list is a business rule, not a validation key — it raises VTF-INV-067.
    public const string AdjustmentBatchRequired = "validation.adjustment.batch.required";
    public const string AdjustmentDirectionRequired = "validation.adjustment.direction.required";
    public const string AdjustmentQuantityPositive = "validation.adjustment.quantity.positive";
    public const string AdjustmentReasonRequired = "validation.adjustment.reason.required";

    // Returns (Epic 2 · C5/C6 — REQ-PUR-006, REQ-SAL-004). The original invoice, the return date,
    // the original line and a positive quantity are each required in their own right, so a
    // rejection names the field the form must highlight. Deliberately absent: a reason key —
    // returns carry no reason at all (BR-INV-067) — and a batch key, because the batch is derived
    // from the original line, never submitted (BR-PUR-017 / BR-SAL-017). The returnable *ceiling*
    // is a business rule, not a validation key: it raises VTF-PUR-016.
    public const string ReturnOriginalInvoiceRequired = "validation.return.originalInvoice.required";
    public const string ReturnDateRequired = "validation.return.date.required";
    public const string ReturnOriginalLineRequired = "validation.return.originalLine.required";
    public const string ReturnQuantityPositive = "validation.return.quantity.positive";
}
