using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.CommitSalesReturn;

/// <summary>
/// Commit a draft sales return (BR-SAL-018, AC-SAL-019): the one-time, all-or-nothing Draft →
/// Committed transition that also applies the stock effect — the batch increases, the on-hand
/// increase and one ledger row per batch — in a single unit of work (BR-INV-062).
///
/// <para>The command carries only the id: everything the commit needs is already on the document
/// and in the recorded consumption trace, and a return has no reason (BR-INV-067) and no amount
/// (DEC-INV-035) to supply.</para>
/// </summary>
public sealed record CommitSalesReturnCommand : ICommand<bool>
{
    public Guid SalesReturnId { get; init; }
}
