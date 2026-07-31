namespace VetFlow.Domain.Inventory;

/// <summary>
/// One row of the unified inventory movement ledger (REQ-INV-009, BR-INV-062..067) — written for
/// <b>every</b> change to an inventory quantity, inside the same unit of work that made the change,
/// because what is not written then can never be reconstructed.
///
/// <para><b>The governing constraint (BR-INV-063, owner ruling 2026-07-31): the ledger records
/// history, it does not calculate inventory.</b> It is an internal implementation detail and is
/// never the source of truth for quantities — <see cref="InventoryBatch.RemainingQuantity"/> and
/// <see cref="ProductOnHand.OnHandQuantity"/> remain the authoritative state exactly as
/// BR-INV-001/002/005 define them. Nothing derives a displayed or computed quantity from this
/// table, and there is no event sourcing (DEC-INV-028). Should the ledger and the quantities ever
/// disagree, the quantities win.</para>
///
/// <para><b>Append-only</b> (DEC-INV-037): there is no mutator and no delete path. A mistake is
/// corrected by writing an opposing movement, never by editing history.</para>
///
/// It absorbs the single-purpose <c>InventoryConsumption</c> record from Sprint 7 (DEC-INV-027)
/// rather than keeping duplicated state: a Consume movement carries the sale line in
/// <see cref="ReferenceId"/>, which preserves the sale-line-level traceability REQ-INV-008 and
/// BR-INV-057 require. <see cref="ReferenceId"/> is a plain <see cref="Guid"/> with no
/// cross-module foreign key — the precedent of <see cref="InventoryBatch.PurchaseLineId"/>.
/// </summary>
public sealed class InventoryMovement
{
    private InventoryMovement()
    {
        // EF Core materialization only.
    }

    private InventoryMovement(
        Guid id,
        Guid productId,
        Guid batchId,
        InventoryMovementType type,
        InventoryMovementSource source,
        decimal quantity,
        Guid? referenceId,
        InventoryMovementReason? reason,
        string? reasonNote,
        string? actorName,
        DateTimeOffset occurredAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(productId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(batchId, Guid.Empty);
        // A zero-quantity movement records nothing and would pollute the history (BR-INV-064:
        // the quantity is signed, and the sign is the direction).
        ArgumentOutOfRangeException.ThrowIfEqual(quantity, 0m);

        Id = id;
        ProductId = productId;
        BatchId = batchId;
        Type = type;
        Source = source;
        Quantity = quantity;
        ReferenceId = referenceId;
        Reason = reason;
        ReasonNote = Trimmed(reasonNote);
        ActorName = Trimmed(actorName);
        OccurredAt = occurredAt;
    }

    public Guid Id { get; }

    public Guid ProductId { get; }

    public Guid BatchId { get; }

    public InventoryMovementType Type { get; }

    /// <summary>The module that caused the movement (BR-INV-064).</summary>
    public InventoryMovementSource Source { get; }

    /// <summary>
    /// Signed quantity in the product's canonical stock unit (BR-INV-002/064): positive increases
    /// stock, negative decreases it. Never rounded (BR-INV-058).
    /// </summary>
    public decimal Quantity { get; }

    /// <summary>
    /// The document or line that caused the movement — a purchase line, a sale line, a return
    /// line. Null for inventory-native operations, which have no counterparty document
    /// (DEC-INV-036).
    /// </summary>
    public Guid? ReferenceId { get; }

    /// <summary>Required for adjustments and write-offs, absent otherwise (BR-INV-067).</summary>
    public InventoryMovementReason? Reason { get; }

    /// <summary>Optional free-text note accompanying the reason code (BR-INV-067).</summary>
    public string? ReasonNote { get; }

    /// <summary>
    /// Optional free-text actor captured at operation time (BR-INV-066, DEC-INV-030). There is no
    /// users module and no authentication in the MVP by explicit owner ruling, so this is never
    /// validated and its absence never blocks an operation — the free-text-supplier precedent
    /// (BR-PUR-001).
    /// </summary>
    public string? ActorName { get; }

    public DateTimeOffset OccurredAt { get; }

    /// <summary>Stock entering inventory: receiving, a sales return, or a positive adjustment.</summary>
    public static InventoryMovement Increase(
        Guid id,
        Guid productId,
        Guid batchId,
        InventoryMovementType type,
        InventoryMovementSource source,
        decimal quantity,
        DateTimeOffset occurredAt,
        Guid? referenceId = null,
        InventoryMovementReason? reason = null,
        string? reasonNote = null,
        string? actorName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        return new InventoryMovement(
            id, productId, batchId, type, source, quantity, referenceId, reason, reasonNote, actorName, occurredAt);
    }

    /// <summary>
    /// Stock leaving inventory: consumption, write-off, a purchase return, or a negative
    /// adjustment. The caller passes the magnitude; the ledger stores it negative, so the sign
    /// convention lives in one place.
    /// </summary>
    public static InventoryMovement Decrease(
        Guid id,
        Guid productId,
        Guid batchId,
        InventoryMovementType type,
        InventoryMovementSource source,
        decimal quantity,
        DateTimeOffset occurredAt,
        Guid? referenceId = null,
        InventoryMovementReason? reason = null,
        string? reasonNote = null,
        string? actorName = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        return new InventoryMovement(
            id, productId, batchId, type, source, -quantity, referenceId, reason, reasonNote, actorName, occurredAt);
    }

    private static string? Trimmed(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
