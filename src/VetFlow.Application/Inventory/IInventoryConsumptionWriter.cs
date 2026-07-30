namespace VetFlow.Application.Inventory;

/// <summary>
/// The Inventory module's public <b>consumption</b> contract (REQ-INV-006, DEC-INV-019) — the
/// second and last inventory movement (BR-INV-055), and the single operation committing a sale
/// depends on (BR-SAL-010, DEC-SAL-006). It is the exact mirror of
/// <see cref="IInventoryReceiptWriter"/> on the outbound side.
///
/// <b>Sales expresses intent; Inventory performs execution.</b> The caller states "consume N of
/// product P for sale line L" and receives success or rejection: it never selects a batch, never
/// sees one, and never learns the FEFO order (BR-SAL-013).
///
/// Inventory: aggregates the requests per product while preserving each quantity's attribution to
/// its sale line (BR-INV-046), reads the <b>saleable</b> batches — active and not expired by the
/// clinic local date (BR-INV-050, BR-INV-059, DEC-INV-021) — in one ordered query (BR-INV-053),
/// checks sufficiency, allocates first-expired-first, then <b>stages</b> the batch decrements, the
/// on-hand decreases (BR-INV-047), and the traceability records (BR-INV-057) onto the current unit
/// of work. It does <b>not</b> commit: the caller's single SaveChanges makes the whole sale atomic
/// (BR-INV-048), and a concurrency conflict on an allocated batch surfaces there (BR-INV-056).
///
/// When the result is a rejection, <b>nothing has been staged</b> — not even for the products that
/// had enough stock (BR-INV-052).
/// </summary>
public interface IInventoryConsumptionWriter
{
    Task<InventoryConsumptionResult> StageAsync(
        IReadOnlyCollection<InventoryConsumptionRequest> requests,
        CancellationToken cancellationToken);
}
