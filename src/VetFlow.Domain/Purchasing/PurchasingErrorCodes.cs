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

    // --- Purchase returns (Epic 2 · C5 — REQ-PUR-006, DEC-PUR-010) ---
    // These four are numbered to match the business rule each one carries, which is the convention
    // this class documents. BR-PUR-017 (the batch is the original line's, never selected) gets no
    // code on purpose: it is not a rejection a user can trigger — the destination is derived, so
    // there is no wrong input to reject.

    /// <summary>BR-PUR-015 — a return may only be created against a <b>Received</b> purchase invoice.</summary>
    public const string ReturnOriginalInvoiceNotReceived = "VTF-PUR-015";

    /// <summary>BR-PUR-016 — the returned quantity exceeds what remains returnable on the original line.</summary>
    public const string ReturnQuantityExceedsReturnable = "VTF-PUR-016";

    /// <summary>BR-PUR-016 — a return line is malformed (a non-positive quantity).</summary>
    public const string ReturnLineComposition = "VTF-PUR-017";

    /// <summary>BR-PUR-018 — only a draft return may change or be committed; a committed return is immutable.</summary>
    public const string ReturnNotDraft = "VTF-PUR-018";

    /// <summary>BR-PUR-018 — a return with no lines cannot be committed.</summary>
    public const string ReturnHasNoLines = "VTF-PUR-019";
}
