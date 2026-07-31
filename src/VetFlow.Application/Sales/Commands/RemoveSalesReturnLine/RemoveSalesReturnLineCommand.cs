using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.RemoveSalesReturnLine;

/// <summary>
/// Remove a line from a draft sales return (BR-SAL-018). There is no edit path — correcting a line
/// is remove then add, the BR-SAL-004 precedent. Only a draft may change; a committed return
/// rejects this with VTF-SAL-018.
/// </summary>
public sealed record RemoveSalesReturnLineCommand : ICommand<bool>
{
    public Guid SalesReturnId { get; init; }

    public Guid SalesReturnLineId { get; init; }
}
