namespace VetFlow.Domain.Catalog;

/// <summary>
/// A desired unit-profile row supplied to <see cref="Product.Update"/> — the
/// editable subset of a <see cref="ProductUnit"/> (BR-CAT-016/018/024). It carries
/// no selling price on purpose: price editing is a separately audited path
/// (REQ-CAT-027 / BR-CAT-028) deferred out of the non-audited edit slice
/// (DEC-CAT-031). On update the aggregate preserves each retained unit's existing
/// price by <see cref="UnitId"/>; the draft can never mutate it.
/// </summary>
public sealed record ProductUnitDraft(
    Guid UnitId,
    int Position,
    decimal? QuantityInNextUnit,
    bool IsPurchaseUnit,
    bool IsSaleUnit,
    string? Barcode);
