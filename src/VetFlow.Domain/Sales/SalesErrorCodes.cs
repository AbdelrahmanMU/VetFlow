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
}
