namespace VetFlow.Application.Inventory;

/// <summary>
/// One consumption request handed to the Inventory module (REQ-INV-006, BR-INV-046): "consume
/// <see cref="StockQuantity"/> of <see cref="ProductId"/>, on behalf of sale line
/// <see cref="SaleLineId"/>".
///
/// The quantity is <b>already in the product's canonical stock unit</b> — the caller converts it
/// before calling, exactly as receiving does (BR-SAL-010, BR-PUR-010), and the conversion is exact
/// because the stock unit is the smallest measurable unit (BR-INV-058); a non-exact conversion is
/// rejected by the caller, never rounded.
///
/// <see cref="SaleLineId"/> is <b>not optional</b>: traceability at sale-line level is a
/// precondition of accepting the request (BR-INV-046, BR-INV-057, REQ-INV-008) and cannot be
/// reconstructed afterwards. Carrying it does not make Sales aware of batches — the direction is
/// the opposite: Sales identifies its line, Inventory attaches the batch (BR-SAL-013).
///
/// Carries no Sales or Catalog type (module isolation): only ids and values.
/// </summary>
public sealed record InventoryConsumptionRequest
{
    public required Guid ProductId { get; init; }

    /// <summary>Quantity in the product's canonical stock unit (already converted, exact).</summary>
    public required decimal StockQuantity { get; init; }

    /// <summary>The sale line this quantity belongs to — the traceability anchor (REQ-INV-008).</summary>
    public required Guid SaleLineId { get; init; }
}
