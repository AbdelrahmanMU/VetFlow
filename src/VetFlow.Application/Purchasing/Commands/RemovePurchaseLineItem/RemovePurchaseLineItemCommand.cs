using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Commands.RemovePurchaseLineItem;

/// <summary>
/// Remove a line item from a draft purchase invoice (REQ-PUR-004, BR-PUR-005) and
/// recompute the invoice total (BR-PUR-006). Both ids arrive as route guids. A
/// <c>null</c> result means the invoice or the line does not exist (404, AC-PUR-010);
/// a non-null result echoes the removed line id.
/// </summary>
public sealed record RemovePurchaseLineItemCommand : ICommand<Guid?>
{
    public required Guid InvoiceId { get; init; }

    public required Guid LineId { get; init; }
}
