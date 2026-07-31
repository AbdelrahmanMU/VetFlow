namespace VetFlow.Application.Sales.Queries.SalesReturnableLines;

/// <summary>
/// One original sale line as the return screen sees it (ui.md §مرتجع مبيعات جديد): the product,
/// what was sold, and what may still be returned.
///
/// <para><b>No unit price and no line total</b> — a return has no financial effect at all
/// (DEC-INV-035) and cash refunds stay out of scope (DEC-SAL-001), so showing money here would
/// imply a refund that does not exist. The temptation is real, because the sale line <i>does</i>
/// carry a price; <c>ui.md</c> lists the four columns this screen has, and none of them is an
/// amount. <b>No batch either</b>: the destination is derived from the consumption trace
/// (BR-SAL-017) and Sales may not hold a batch reference at all (BR-SAL-013).</para>
/// </summary>
public sealed record SalesReturnableLineDto
{
    public required Guid SalesLineItemId { get; init; }

    public required Guid ProductId { get; init; }

    public required string ProductName { get; init; }

    public required string SaleUnitName { get; init; }

    /// <summary>The quantity originally sold on this line, in its sale unit («الكمّية المباعة»).</summary>
    public required decimal Quantity { get; init; }

    /// <summary>Already returned across every committed return of this invoice (BR-SAL-016).</summary>
    public required decimal ReturnedQuantity { get; init; }

    /// <summary>Quantity − returned; what a new return line may still take («المتبقّي القابل للإرجاع» — BR-SAL-016).</summary>
    public required decimal ReturnableQuantity { get; init; }
}
