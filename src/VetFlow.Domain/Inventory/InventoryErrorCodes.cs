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
}
