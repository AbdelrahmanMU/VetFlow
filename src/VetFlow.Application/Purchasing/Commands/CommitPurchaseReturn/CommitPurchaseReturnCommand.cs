using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Commands.CommitPurchaseReturn;

/// <summary>
/// Commit a draft purchase return (BR-PUR-018, AC-PUR-023): the one-time, all-or-nothing
/// Draft → Committed transition that also applies the stock effect — the batch decrement, the
/// on-hand decrement and one ledger row per line — in a single unit of work (BR-INV-062).
///
/// <para>The command carries only the id: everything the commit needs is already on the document,
/// and a return has no reason (BR-INV-067) and no amount (DEC-INV-035) to supply.</para>
/// </summary>
public sealed record CommitPurchaseReturnCommand : ICommand<bool>
{
    public Guid PurchaseReturnId { get; init; }
}
