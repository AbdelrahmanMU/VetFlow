namespace VetFlow.Domain.Inventory;

/// <summary>
/// Inventory error codes. Each code maps to exactly one business rule and the numbering is aligned
/// with the module's BR-INV-NNN identifiers (ADR-0018) — the Purchasing precedent.
/// </summary>
public static class InventoryErrorCodes
{
    /// <summary>BR-INV-046 — a consumption request is malformed (non-positive quantity, or no sale line to attribute it to).</summary>
    public const string ConsumptionRequestInvalid = "VTF-INV-046";

    /// <summary>BR-INV-052 — the saleable stock does not cover the request; the whole operation is rejected without any effect.</summary>
    public const string InsufficientStock = "VTF-INV-052";

    /// <summary>BR-INV-056 — an allocated batch changed between allocation and commit; the sale fails and must be retried.</summary>
    public const string ConcurrencyConflict = "VTF-INV-056";

    /// <summary>
    /// BR-INV-061 — the operation would drive a batch's remaining quantity below zero. The whole
    /// operation is rejected; nothing is clamped and nothing is applied partially (DEC-INV-032).
    /// </summary>
    public const string QuantityBelowZero = "VTF-INV-061";

    /// <summary>
    /// BR-INV-067 — the reason code does not belong to the operation's own list. Adjustments and
    /// write-offs draw from two deliberately different sets (DEC-INV-031).
    /// </summary>
    public const string ReasonNotAllowed = "VTF-INV-067";

    /// <summary>
    /// BR-INV-068 — a batch changed while a stock-decreasing operation was being written. Distinct
    /// from <see cref="ConcurrencyConflict"/>, which BR-INV-056 scopes to sale consumption: the
    /// outcome is the same (fail, never write silently, retry) but the rule is a different one, and
    /// STD-BE-033 requires one code per rule.
    /// </summary>
    public const string OperationConcurrencyConflict = "VTF-INV-068";
}
