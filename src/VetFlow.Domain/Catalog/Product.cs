using VetFlow.Domain.Common;

namespace VetFlow.Domain.Catalog;

/// <summary>
/// The product master-data aggregate — the single definition of what the clinic
/// sells (catalog overview). Creation enforces the documented minimum
/// (BR-CAT-009) and the unit-profile rules (BR-CAT-016 … BR-CAT-025).
/// </summary>
public sealed class Product
{
    private readonly List<ProductUnit> _units = [];

    private Product()
    {
        // EF Core materialization only.
        ArabicName = string.Empty;
    }

    public Product(
        Guid id,
        string internalCode,
        string arabicName,
        Guid categoryId,
        Guid manufacturerId,
        Guid natureId,
        ProductCapabilities capabilities,
        IReadOnlyCollection<ProductUnit> units,
        Guid storageUnitId,
        Guid defaultSaleUnitId,
        Guid defaultPurchaseUnitId,
        string? englishName = null,
        string? size = null,
        string? concentration = null,
        string? internalNotes = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        // The internal code is system-generated at persist time (BR-CAT-006,
        // DEC-CAT-026); an absent code here is a programmer error, not a business
        // failure — the PRD- format itself is owned by Infrastructure.
        ArgumentException.ThrowIfNullOrWhiteSpace(internalCode);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(units);

        if (string.IsNullOrWhiteSpace(arabicName))
        {
            throw MissingMinimum("arabicName");
        }

        if (categoryId == Guid.Empty)
        {
            throw MissingMinimum("category");
        }

        if (manufacturerId == Guid.Empty)
        {
            throw MissingMinimum("manufacturer");
        }

        if (natureId == Guid.Empty)
        {
            throw MissingMinimum("nature");
        }

        if (units.Count == 0)
        {
            throw MissingMinimum("unitProfile");
        }

        if (!units.Any(unit => unit.IsPurchaseUnit))
        {
            throw MissingMinimum("purchaseUnit");
        }

        if (!units.Any(unit => unit.IsSaleUnit))
        {
            throw MissingMinimum("saleUnit");
        }

        if (units.Select(unit => unit.UnitId).Distinct().Count() != units.Count)
        {
            throw new BusinessRuleException(
                CatalogErrorCodes.UnitProfileComposition,
                new Dictionary<string, string> { ["reason"] = "duplicateUnit" });
        }

        if (units.All(unit => unit.UnitId != storageUnitId))
        {
            throw new BusinessRuleException(CatalogErrorCodes.StorageUnitNotInProfile);
        }

        var defaultSaleUnit = units.FirstOrDefault(unit => unit.UnitId == defaultSaleUnitId);
        if (defaultSaleUnit is null || !defaultSaleUnit.IsSaleUnit)
        {
            throw new BusinessRuleException(CatalogErrorCodes.DefaultSaleUnitInvalid);
        }

        var defaultPurchaseUnit = units.FirstOrDefault(unit => unit.UnitId == defaultPurchaseUnitId);
        if (defaultPurchaseUnit is null || !defaultPurchaseUnit.IsPurchaseUnit)
        {
            throw new BusinessRuleException(CatalogErrorCodes.DefaultPurchaseUnitInvalid);
        }

        Id = id;
        InternalCode = internalCode;
        ArabicName = arabicName.Trim();
        EnglishName = string.IsNullOrWhiteSpace(englishName) ? null : englishName.Trim();
        InternalNotes = string.IsNullOrWhiteSpace(internalNotes) ? null : internalNotes.Trim();
        Size = string.IsNullOrWhiteSpace(size) ? null : size.Trim();
        Concentration = string.IsNullOrWhiteSpace(concentration) ? null : concentration.Trim();
        CategoryId = categoryId;
        ManufacturerId = manufacturerId;
        NatureId = natureId;
        IsSplittable = capabilities.IsSplittable;
        IsRefrigerated = capabilities.IsRefrigerated;
        HasExpiration = capabilities.HasExpiration;
        HasOpenExpiration = capabilities.HasOpenExpiration;
        OpenExpirationPeriod = capabilities.OpenExpirationPeriod;
        StorageUnitId = storageUnitId;
        DefaultSaleUnitId = defaultSaleUnitId;
        DefaultPurchaseUnitId = defaultPurchaseUnitId;
        Status = ProductStatus.Active;
        _units.AddRange(units);
    }

    public Guid Id { get; }

    /// <summary>
    /// System-generated internal code (BR-CAT-006, DEC-CAT-026): a stable
    /// reporting/audit/support reference, never a search key (DEC-CAT-016).
    /// Assigned once at creation from a unique ascending sequence; never changes.
    /// </summary>
    public string InternalCode { get; private set; } = string.Empty;

    /// <summary>Mandatory; the product's displayed name everywhere (BR-CAT-005).</summary>
    public string ArabicName { get; private set; }

    public string? EnglishName { get; private set; }

    /// <summary>Optional internal notes for the clinic team; never shown on invoices (BR-CAT-050).</summary>
    public string? InternalNotes { get; private set; }

    /// <summary>Explicit optional identity field (BR-CAT-008).</summary>
    public string? Size { get; private set; }

    /// <summary>Explicit optional identity field (BR-CAT-008).</summary>
    public string? Concentration { get; private set; }

    /// <summary>Exactly one category (BR-CAT-012); organization only, never behavior (BR-CAT-015).</summary>
    public Guid CategoryId { get; private set; }

    /// <summary>Mandatory managed manufacturer (BR-CAT-007).</summary>
    public Guid ManufacturerId { get; private set; }

    /// <summary>Mandatory product nature — the behavioral driver (BR-CAT-014).</summary>
    public Guid NatureId { get; private set; }

    public bool IsSplittable { get; private set; }

    public bool IsRefrigerated { get; private set; }

    public bool HasExpiration { get; private set; }

    public bool HasOpenExpiration { get; private set; }

    public TimeSpan? OpenExpirationPeriod { get; private set; }

    /// <summary>The single stock-keeping unit all quantities are computed in (BR-CAT-020).</summary>
    public Guid StorageUnitId { get; private set; }

    /// <summary>Auto-selected sale unit, changeable by the cashier (BR-CAT-022).</summary>
    public Guid DefaultSaleUnitId { get; private set; }

    /// <summary>Auto-selected purchase unit, changeable per invoice line (BR-CAT-021).</summary>
    public Guid DefaultPurchaseUnitId { get; private set; }

    public ProductStatus Status { get; private set; }

    public IReadOnlyCollection<ProductUnit> Units => _units.AsReadOnly();

    private static BusinessRuleException MissingMinimum(string field) =>
        new(CatalogErrorCodes.MinimumProductData, new Dictionary<string, string> { ["field"] = field });
}
