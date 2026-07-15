using VetFlow.Domain.Common;

namespace VetFlow.Domain.Catalog;

/// <summary>
/// The product master-data aggregate — the single definition of what the clinic
/// sells (catalog overview). Creation enforces the documented minimum
/// (BR-CAT-009) and the unit-profile rules (BR-CAT-016 … BR-CAT-025); the same
/// invariants are re-enforced on <see cref="Update"/> (REQ-CAT-038 first clause,
/// BR-CAT-041). Creation and editing share one invariant gate — there is no
/// second, weaker path.
/// </summary>
public sealed class Product
{
    private readonly List<ProductUnit> _units = [];

    private Product()
    {
        // EF Core materialization only.
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

        ValidateInvariants(arabicName, categoryId, manufacturerId, natureId, units, storageUnitId, defaultSaleUnitId, defaultPurchaseUnitId);

        Id = id;
        InternalCode = internalCode;
        ApplyDetails(arabicName, englishName, size, concentration, categoryId, manufacturerId, natureId, capabilities, storageUnitId, defaultSaleUnitId, defaultPurchaseUnitId, internalNotes);
        Status = ProductStatus.Active;
        _units.AddRange(units);
    }

    public Guid Id { get; }

    /// <summary>
    /// System-generated internal code (BR-CAT-006, DEC-CAT-026): a stable
    /// reporting/audit/support reference, never a search key (DEC-CAT-016).
    /// Assigned once at creation from a unique ascending sequence; never changes —
    /// <see cref="Update"/> deliberately does not touch it.
    /// </summary>
    public string InternalCode { get; private set; } = string.Empty;

    /// <summary>Mandatory; the product's displayed name everywhere (BR-CAT-005).</summary>
    public string ArabicName { get; private set; } = string.Empty;

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

    /// <summary>
    /// Non-audited edit of the product's basic data (REQ-CAT-038 first clause,
    /// BR-CAT-041, REQ-CAT-003): identity, classification, capabilities, notes, and
    /// the unit profile — replacing the profile with <paramref name="unitDrafts"/>.
    /// The same invariant gate as creation runs (BR-CAT-009/016/020/022). The
    /// internal code and status are never touched. Selling prices are never mutated
    /// here (DEC-CAT-031): each retained unit keeps its existing price by
    /// <see cref="ProductUnit.UnitId"/>; a newly added unit starts with no price;
    /// a unit that stops being a sale unit drops its price (a non-sale unit cannot
    /// hold one — BR-CAT-025).
    /// </summary>
    public void Update(
        string arabicName,
        string? englishName,
        string? size,
        string? concentration,
        Guid categoryId,
        Guid manufacturerId,
        Guid natureId,
        ProductCapabilities capabilities,
        string? internalNotes,
        IReadOnlyCollection<ProductUnitDraft> unitDrafts,
        Guid storageUnitId,
        Guid defaultSaleUnitId,
        Guid defaultPurchaseUnitId)
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(unitDrafts);

        var retainedPrices = _units
            .Where(unit => unit.SellingPrice is not null)
            .ToDictionary(unit => unit.UnitId, unit => unit.SellingPrice);

        var replacement = unitDrafts
            .Select(draft => new ProductUnit(
                Guid.NewGuid(),
                draft.UnitId,
                draft.Position,
                draft.QuantityInNextUnit,
                draft.IsPurchaseUnit,
                draft.IsSaleUnit,
                draft.Barcode,
                draft.IsSaleUnit && retainedPrices.TryGetValue(draft.UnitId, out var price) ? price : null))
            .ToList();

        ValidateInvariants(arabicName, categoryId, manufacturerId, natureId, replacement, storageUnitId, defaultSaleUnitId, defaultPurchaseUnitId);

        ApplyDetails(arabicName, englishName, size, concentration, categoryId, manufacturerId, natureId, capabilities, storageUnitId, defaultSaleUnitId, defaultPurchaseUnitId, internalNotes);

        _units.Clear();
        _units.AddRange(replacement);

        // TODO(DEC-CAT-028 / DEC-CAT-029 / DEC-CAT-031): the *dangerous* unit-profile
        // change (a conversion factor or the storage unit for a product that already
        // has stock or movements — REQ-CAT-038 second clause, BR-CAT-041) additionally
        // requires explicit user confirmation and an audit-log entry. No stock/movement
        // source exists yet (DEC-CAT-025 pattern) and the audit-log record shape is a
        // pending owner decision (DEC-CAT-028), so this path is intentionally inert:
        // with no stock, every unit-profile edit is a free basic edit. When a stock
        // source and the audit shape land, gate the dangerous delta here.
    }

    /// <summary>
    /// Sets the mutable scalar state shared by creation and update. Trims optional
    /// text and normalizes empty to null so both paths behave identically (no
    /// duplicated mapping).
    /// </summary>
    private void ApplyDetails(
        string arabicName,
        string? englishName,
        string? size,
        string? concentration,
        Guid categoryId,
        Guid manufacturerId,
        Guid natureId,
        ProductCapabilities capabilities,
        Guid storageUnitId,
        Guid defaultSaleUnitId,
        Guid defaultPurchaseUnitId,
        string? internalNotes)
    {
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
    }

    /// <summary>
    /// The single minimum-data and unit-profile invariant gate (BR-CAT-009/016/020/
    /// 021/022). Shared by creation and update so both paths enforce exactly the same
    /// rules with the same error codes.
    /// </summary>
    private static void ValidateInvariants(
        string arabicName,
        Guid categoryId,
        Guid manufacturerId,
        Guid natureId,
        IReadOnlyCollection<ProductUnit> units,
        Guid storageUnitId,
        Guid defaultSaleUnitId,
        Guid defaultPurchaseUnitId)
    {
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
    }

    private static BusinessRuleException MissingMinimum(string field) =>
        new(CatalogErrorCodes.MinimumProductData, new Dictionary<string, string> { ["field"] = field });
}
