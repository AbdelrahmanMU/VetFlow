namespace VetFlow.Domain.Purchasing;

/// <summary>
/// Purchasing error codes. Each code maps to exactly one business rule and the
/// numbering is aligned with the module's BR-PUR-NNN identifiers (ADR-0018).
/// </summary>
public static class PurchasingErrorCodes
{
    /// <summary>BR-PUR-003 / BR-PUR-005 / BR-PUR-009 — only a draft invoice may change (add/remove a line) or be received.</summary>
    public const string InvoiceNotDraft = "VTF-PUR-003";

    /// <summary>BR-PUR-005 — a purchase line is malformed (quantity, unit price, or the product/unit reference).</summary>
    public const string LineComposition = "VTF-PUR-005";

    /// <summary>BR-PUR-009 / BR-PUR-012 — a purchase invoice with no lines cannot be received.</summary>
    public const string InvoiceHasNoLines = "VTF-PUR-006";

    /// <summary>BR-PUR-013 / DEC-PUR-009 — a received line whose product requires expiry was given no expiry date.</summary>
    public const string ExpiryRequired = "VTF-PUR-007";
}
