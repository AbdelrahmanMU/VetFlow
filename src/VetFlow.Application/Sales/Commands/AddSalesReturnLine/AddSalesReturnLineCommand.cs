using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.AddSalesReturnLine;

/// <summary>
/// Add a line to a draft sales return (REQ-SAL-004, BR-SAL-016): which original sale line is being
/// returned against, and how much.
///
/// <para><b>No batch is accepted, and none could be.</b> One sale line may have left through
/// several batches (FEFO splits — BR-SAL-017), the destination is read from the recorded
/// consumption trace at commit (BR-INV-069), and Sales may not hold a batch reference at all
/// (BR-SAL-013). Accepting a batch id here would expose a choice that does not exist and let a
/// caller push stock into a batch the goods never left.</para>
///
/// <para>Partial returns are allowed (owner ruling, DEC-SAL-010): the quantity may be less than the
/// original line's, capped at what remains returnable — which the handler derives from the
/// committed returns (BR-SAL-016) — and it respects the product's splittability exactly as the sale
/// did (DEC-SAL-007).</para>
/// </summary>
public sealed record AddSalesReturnLineCommand : ICommand<Guid?>
{
    public Guid SalesReturnId { get; init; }

    public Guid? SalesLineItemId { get; init; }

    public decimal? Quantity { get; init; }
}
