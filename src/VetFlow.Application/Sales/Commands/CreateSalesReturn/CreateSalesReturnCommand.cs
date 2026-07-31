using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.CreateSalesReturn;

/// <summary>
/// Create a sales return (REQ-SAL-004, DEC-SAL-010): the header only — the single originating sales
/// invoice (BR-SAL-015), the required return date, and optional notes. The <c>SRT-</c> number
/// (BR-SAL-014) is generated at persist time and is never supplied here, and the return is always
/// born a draft (BR-SAL-018).
///
/// <para>There is no customer field: the customer is snapshotted from the originating invoice, so
/// accepting one here would let the two disagree. There is no reason field (BR-INV-067) and no
/// amount field (DEC-INV-035).</para>
/// </summary>
public sealed record CreateSalesReturnCommand : ICommand<CreateSalesReturnResult?>
{
    /// <summary>The one original invoice (BR-SAL-015); it must be <c>Committed</c>, checked by the handler.</summary>
    public Guid? SalesInvoiceId { get; init; }

    /// <summary>Required; validated <c>NotNull</c> so a missing date is a per-field 400.</summary>
    public DateOnly? ReturnDate { get; init; }

    public string? Notes { get; init; }
}
