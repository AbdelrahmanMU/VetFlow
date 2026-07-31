namespace VetFlow.Domain.Sales;

/// <summary>
/// Sales error codes. Each code maps to exactly one business rule and the numbering is aligned
/// with the module's BR-SAL-NNN identifiers (ADR-0018) — the Purchasing precedent.
/// </summary>
public static class SalesErrorCodes
{
    /// <summary>BR-SAL-003 / BR-SAL-004 / BR-SAL-011 — only a draft invoice may change its lines or be committed.</summary>
    public const string InvoiceNotDraft = "VTF-SAL-003";

    /// <summary>BR-SAL-004 — a sales line is malformed (quantity, price, the product/unit reference, or the splittability constraint).</summary>
    public const string LineComposition = "VTF-SAL-004";

    /// <summary>BR-SAL-009 / BR-SAL-012 — a sales invoice with no lines cannot be committed.</summary>
    public const string InvoiceHasNoLines = "VTF-SAL-009";

    /// <summary>BR-SAL-012 / BR-INV-058 — a line quantity does not convert into the product's stock unit exactly; rejected, never rounded.</summary>
    public const string InexactUnitConversion = "VTF-SAL-012";

    // --- Sales returns (Epic 2 · C6 — REQ-SAL-004, DEC-SAL-010) ---
    // Numbered to match the business rule each one carries, the convention this class documents and
    // the exact shape C5 gave Purchasing. BR-SAL-017 (the destination batches are read from the
    // consumption trace, never selected) gets no code on purpose: it is not a rejection a user can
    // trigger — the destination is derived, so there is no wrong input to reject.

    /// <summary>BR-SAL-015 — a return may only be created against a <b>Committed</b> sales invoice.</summary>
    public const string ReturnOriginalInvoiceNotCommitted = "VTF-SAL-015";

    /// <summary>BR-SAL-016 — the returned quantity exceeds what remains returnable on the original line.</summary>
    public const string ReturnQuantityExceedsReturnable = "VTF-SAL-016";

    /// <summary>BR-SAL-016 — a return line is malformed (a non-positive quantity, or a fractional quantity of an indivisible product).</summary>
    public const string ReturnLineComposition = "VTF-SAL-017";

    /// <summary>BR-SAL-018 — only a draft return may change or be committed; a committed return is immutable.</summary>
    public const string ReturnNotDraft = "VTF-SAL-018";

    /// <summary>BR-SAL-018 — a return with no lines cannot be committed.</summary>
    public const string ReturnHasNoLines = "VTF-SAL-019";

    /// <summary>
    /// BR-SAL-017 — the sale line's consumption trace cannot support the return: it is missing
    /// entirely, or it records less stock than the return would put back. Not a user input error —
    /// it means the document and the ledger disagree, so the commit fails loudly with nothing saved
    /// rather than moving a quantity nobody can justify.
    /// </summary>
    public const string ReturnConsumptionTraceUnusable = "VTF-SAL-020";
}
