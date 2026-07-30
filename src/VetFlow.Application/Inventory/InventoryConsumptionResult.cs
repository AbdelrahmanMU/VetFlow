namespace VetFlow.Application.Inventory;

/// <summary>
/// What the caller learns from a consumption attempt (REQ-INV-006): <b>success, or rejection with
/// its reason</b> — and nothing else. No batch list, no allocation detail, no ordering: the
/// isolation boundary is preserved (BR-SAL-013, DEC-INV-019).
///
/// The only rejection Inventory can determine before the write is insufficient <b>saleable</b>
/// stock (BR-INV-052) — measured on non-expired active batches only, so a product whose balance
/// looks positive in the projection can still be reported insufficient (DEC-INV-021). The other
/// rejection, a concurrency conflict (BR-INV-056), surfaces at commit time, not here.
///
/// Products are identified by id: Inventory does not read Catalog names (module isolation); the
/// caller turns the ids into the names the user sees.
/// </summary>
public sealed record InventoryConsumptionResult
{
    private InventoryConsumptionResult()
    {
    }

    /// <summary>The products whose saleable stock did not cover the request; empty on success.</summary>
    public required IReadOnlyList<Guid> InsufficientProductIds { get; init; }

    public bool Succeeded => InsufficientProductIds.Count == 0;

    public static InventoryConsumptionResult Success { get; } =
        new() { InsufficientProductIds = [] };

    /// <summary>Rejected: not enough saleable stock for the listed products (BR-INV-052) — nothing was staged.</summary>
    public static InventoryConsumptionResult Insufficient(IReadOnlyList<Guid> productIds) =>
        new() { InsufficientProductIds = productIds };
}
