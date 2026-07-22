namespace VetFlow.Application.Inventory;

/// <summary>
/// The Inventory write kernel's public contract (write-kernel.md, DEC-INV-001) — the single
/// operation Purchase Receiving depends on (BR-PUR-010, DEC-PUR-008). For each received line it
/// <b>stages</b> one inventory batch (BR-INV-001) and the product's on-hand increase (BR-INV-002)
/// onto the current unit of work; it does <b>not</b> commit. The caller's single SaveChanges makes
/// the whole receipt atomic (BR-INV-003). The kernel owns nothing else — no queries, projection,
/// adjustments, returns, or history (BR-INV-004).
/// </summary>
public interface IInventoryReceiptWriter
{
    Task StageAsync(IReadOnlyCollection<InventoryReceiptLine> lines, CancellationToken cancellationToken);
}
