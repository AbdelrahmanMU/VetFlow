using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Commands.AddPurchaseReturnLine;

/// <summary>
/// Add a line to a draft purchase return (REQ-PUR-006, BR-PUR-016): which original purchase line
/// is being returned against, and how much.
///
/// <para><b>No batch is accepted.</b> One purchase line created exactly one batch (DEC-PUR-008),
/// so the destination is a fact the handler resolves — BR-PUR-017 forbids selecting one. Accepting
/// a batch id here would expose a choice that does not exist and let a caller return stock into
/// the wrong batch.</para>
///
/// <para>Partial returns are allowed (owner ruling, DEC-PUR-010): the quantity may be less than
/// the original line's, capped at what remains returnable — which the handler derives from the
/// committed returns (BR-PUR-016).</para>
/// </summary>
public sealed record AddPurchaseReturnLineCommand : ICommand<Guid?>
{
    public Guid PurchaseReturnId { get; init; }

    public Guid? PurchaseLineItemId { get; init; }

    public decimal? Quantity { get; init; }
}
