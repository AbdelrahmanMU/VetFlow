namespace VetFlow.Application.Purchasing.Queries.PurchaseReturnableLines;

/// <summary>
/// One original purchase line as the return screen sees it (ui.md §مرتجع مشتريات جديد): the
/// product, what was bought, and what may still be returned.
///
/// <para><b>No unit price and no line total</b> — a return has no financial effect at all
/// (DEC-INV-035), so showing money here would imply a credit that does not exist. <b>No batch
/// either</b>: it is derived from the line (BR-PUR-017), and surfacing it as data the screen
/// carries would invite a picker the rules forbid.</para>
/// </summary>
public sealed record PurchaseReturnableLineDto
{
    public required Guid PurchaseLineItemId { get; init; }

    public required Guid ProductId { get; init; }

    public required string ProductName { get; init; }

    public required string PurchaseUnitName { get; init; }

    /// <summary>The quantity originally received on this line.</summary>
    public required decimal Quantity { get; init; }

    /// <summary>Already returned across every committed return of this invoice (BR-PUR-016).</summary>
    public required decimal ReturnedQuantity { get; init; }

    /// <summary>Quantity − returned; what a new return line may still take (BR-PUR-016).</summary>
    public required decimal ReturnableQuantity { get; init; }
}
