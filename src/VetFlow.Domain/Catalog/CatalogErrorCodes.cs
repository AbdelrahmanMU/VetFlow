namespace VetFlow.Domain.Catalog;

/// <summary>
/// Catalog error codes. Each code maps to exactly one business rule and the
/// numbering is aligned with the module's BR-CAT-NNN identifiers (ADR-0018).
/// </summary>
public static class CatalogErrorCodes
{
    /// <summary>BR-CAT-009 — a product misses the mandatory creation minimum.</summary>
    public const string MinimumProductData = "VTF-CAT-009";

    /// <summary>BR-CAT-016 — the unit profile composition is malformed (e.g. duplicated units).</summary>
    public const string UnitProfileComposition = "VTF-CAT-016";

    /// <summary>BR-CAT-020 — the stock-keeping unit must be one unit chosen from the profile.</summary>
    public const string StorageUnitNotInProfile = "VTF-CAT-020";

    /// <summary>BR-CAT-021 — the default purchase unit must be a purchase unit from the profile.</summary>
    public const string DefaultPurchaseUnitInvalid = "VTF-CAT-021";

    /// <summary>BR-CAT-022 — the default sale unit must be a sale unit from the profile.</summary>
    public const string DefaultSaleUnitInvalid = "VTF-CAT-022";

    /// <summary>BR-CAT-025 — a selling price belongs to a sale unit only.</summary>
    public const string PriceOnNonSaleUnit = "VTF-CAT-025";

    /// <summary>BR-CAT-036 — "has open expiration" requires the open-expiration period.</summary>
    public const string OpenExpirationPeriodRequired = "VTF-CAT-036";
}
