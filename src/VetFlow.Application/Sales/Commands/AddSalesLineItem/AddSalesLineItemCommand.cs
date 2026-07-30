using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.AddSalesLineItem;

/// <summary>
/// Add a line to a draft sales invoice (REQ-SAL-001, BR-SAL-004): a product, one of its
/// <b>sale</b> units, and a quantity. The unit price is deliberately <b>absent</b> — it is the
/// catalog sale price captured as a snapshot by the handler at add time, never entered or edited
/// by the user in Sprint 7 (DEC-SAL-003: no discount, no override, no reason, no audit).
/// Returns the new line's id, or <c>null</c> when the invoice does not exist (404).
/// </summary>
public sealed record AddSalesLineItemCommand : ICommand<Guid?>
{
    public required Guid InvoiceId { get; init; }

    public required Guid ProductId { get; init; }

    /// <summary>Must be a sale unit of the product (BR-SAL-004); defaults to the product's default sale unit in the UI.</summary>
    public required Guid SaleUnitId { get; init; }

    public required decimal Quantity { get; init; }
}
