namespace VetFlow.Application.Inventory;

/// <summary>
/// One sales-return request handed to the Inventory module (REQ-SAL-004, BR-SAL-017): "put back the
/// portion of sale line <see cref="SaleLineId"/> that runs from
/// <see cref="PreviouslyReturnedQuantity"/> to <see cref="PreviouslyReturnedQuantity"/> +
/// <see cref="ReturnQuantity"/>, on behalf of return line <see cref="ReturnLineId"/>".
///
/// <para><b>Every quantity here is in the sale line's own sale unit</b> — deliberately, and this is
/// the one place the return contract differs from
/// <see cref="InventoryConsumptionRequest"/>, where Sales converts to stock units before calling.
/// A return must move stock by the factor that <b>actually applied</b> when the goods left, not by
/// today's catalog factor: if a product's conversion were edited after the sale, the catalog would
/// give a factor that never applied to this stock and the return would put back the wrong amount.
/// The only record of the factor that applied is the consumption trace, which is Inventory's
/// (BR-INV-057) — so Inventory does the conversion. This is the same reasoning that made C5 derive
/// its factor from the receipt rather than the catalog (BR-PUR-016).</para>
///
/// <para><see cref="SoldQuantity"/> and <see cref="PreviouslyReturnedQuantity"/> are facts of the
/// <b>Sales</b> documents — the original line's quantity and what earlier <i>committed</i> returns
/// already took from it (BR-SAL-016). Sales owns both and passes them; it still never sees a batch,
/// never chooses one, and never learns the FEFO order (BR-SAL-013).</para>
///
/// Carries no Sales or Catalog type (module isolation): only ids and values.
/// </summary>
public sealed record InventorySalesReturnRequest
{
    /// <summary>The original sale line — the anchor the consumption trace is read by (REQ-INV-008).</summary>
    public required Guid SaleLineId { get; init; }

    /// <summary>The return line causing this movement; it travels into the ledger's reference (BR-INV-057).</summary>
    public required Guid ReturnLineId { get; init; }

    /// <summary>The original sale line's quantity, in its sale unit (BR-SAL-016).</summary>
    public required decimal SoldQuantity { get; init; }

    /// <summary>
    /// Already returned against that line by <b>committed</b> returns, in the same unit
    /// (BR-SAL-016). It is what makes a second partial return continue where the first stopped
    /// instead of refilling a batch that has already been made whole (BR-SAL-017).
    /// </summary>
    public required decimal PreviouslyReturnedQuantity { get; init; }

    /// <summary>What this document returns now, in the same unit; always positive.</summary>
    public required decimal ReturnQuantity { get; init; }
}
