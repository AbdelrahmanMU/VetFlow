namespace VetFlow.Domain.Purchasing;

/// <summary>
/// Purchasing error codes. Each code maps to exactly one business rule and the
/// numbering is aligned with the module's BR-PUR-NNN identifiers (ADR-0018).
/// </summary>
public static class PurchasingErrorCodes
{
    /// <summary>BR-PUR-003 / BR-PUR-005 — only a draft invoice may change (add/remove a line).</summary>
    public const string InvoiceNotDraft = "VTF-PUR-003";

    /// <summary>BR-PUR-005 — a purchase line is malformed (quantity, unit price, or the product/unit reference).</summary>
    public const string LineComposition = "VTF-PUR-005";
}
