namespace VetFlow.Infrastructure.Persistence.Numbering;

/// <summary>
/// The five numbered series (ADR-0022 §6). Each owns a counter per scope; none of them shares one,
/// so a purchase invoice can never consume a sales invoice's number.
/// </summary>
public enum DocumentSeries
{
    /// <summary>Catalog product code, <c>PRD-</c> (BR-CAT-006, DEC-CAT-026).</summary>
    ProductCode,

    /// <summary>Purchase invoice, <c>PUR-</c> (BR-PUR-002).</summary>
    PurchaseInvoice,

    /// <summary>Purchase return, <c>PRT-</c> (BR-PUR-014).</summary>
    PurchaseReturn,

    /// <summary>Sales invoice, <c>SAL-</c> (BR-SAL-002).</summary>
    SalesInvoice,

    /// <summary>Sales return, <c>SRT-</c> (BR-SAL-014).</summary>
    SalesReturn,
}

/// <summary>
/// Which organizational scope each series counts within (ADR-0022 §6).
///
/// <b>A product code counts per tenant</b> — the catalog is shared across a clinic's branches
/// (DEC-ORG-006), so the same product must not acquire a second code at a second branch.
/// <b>Documents count per branch</b>, because they record events that happened at a place, and
/// because a branch is where an accounting series lives.
///
/// §12.13 makes this direction one-way: a scope may later become finer, never wider. Widening
/// branch → tenant would migrate accounting series a bookkeeper can see.
/// </summary>
public static class DocumentSeriesScope
{
    /// <summary>
    /// The stored key for a series. Short and legible in the database, and deliberately not the
    /// enum's name: this value is persisted, so it must not move when a C# identifier is renamed.
    /// </summary>
    public static string Code(this DocumentSeries series) => series switch
    {
        DocumentSeries.ProductCode => "PRD",
        DocumentSeries.PurchaseInvoice => "PUR",
        DocumentSeries.PurchaseReturn => "PRT",
        DocumentSeries.SalesInvoice => "SAL",
        DocumentSeries.SalesReturn => "SRT",
        _ => throw new ArgumentOutOfRangeException(nameof(series), series, "Unknown document series."),
    };

    /// <summary>True when the series counts per branch rather than per tenant.</summary>
    public static bool IsBranchScoped(this DocumentSeries series) => series != DocumentSeries.ProductCode;
}
