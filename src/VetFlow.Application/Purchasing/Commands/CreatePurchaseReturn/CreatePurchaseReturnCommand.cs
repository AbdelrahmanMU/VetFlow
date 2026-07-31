using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Commands.CreatePurchaseReturn;

/// <summary>
/// Create a purchase return (REQ-PUR-006, DEC-PUR-010): the header only — the single originating
/// purchase invoice (BR-PUR-015), the required return date, and optional notes. The
/// <c>PRT-</c> number (BR-PUR-014) is generated at persist time and is never supplied here, and
/// the return is always born a draft (BR-PUR-018).
///
/// <para>There is no supplier field: the supplier is snapshotted from the originating invoice, so
/// accepting one here would let the two disagree. There is no reason field (BR-INV-067) and no
/// amount field (DEC-INV-035).</para>
/// </summary>
public sealed record CreatePurchaseReturnCommand : ICommand<CreatePurchaseReturnResult?>
{
    /// <summary>The one original invoice (BR-PUR-015); it must be <c>Received</c>, checked by the handler.</summary>
    public Guid? PurchaseInvoiceId { get; init; }

    /// <summary>Required; validated <c>NotNull</c> so a missing date is a per-field 400.</summary>
    public DateOnly? ReturnDate { get; init; }

    public string? Notes { get; init; }
}
