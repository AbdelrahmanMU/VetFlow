using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.RemoveSalesLineItem;

/// <summary>
/// Remove a line from a draft sales invoice (REQ-SAL-001, BR-SAL-004). There is no edit path:
/// correcting a line is remove then add. Returns <c>null</c> when the invoice or the line does not
/// exist (404); a committed invoice is rejected (BR-SAL-011).
/// </summary>
public sealed record RemoveSalesLineItemCommand : ICommand<Guid?>
{
    public required Guid InvoiceId { get; init; }

    public required Guid LineId { get; init; }
}
