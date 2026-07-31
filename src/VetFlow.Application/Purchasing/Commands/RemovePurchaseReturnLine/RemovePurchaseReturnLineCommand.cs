using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Commands.RemovePurchaseReturnLine;

/// <summary>
/// Remove a line from a draft purchase return (BR-PUR-018). There is no edit path — correcting a
/// line is remove then add, the BR-PUR-005 precedent. Only a draft may change; a committed return
/// rejects this with VTF-PUR-018.
/// </summary>
public sealed record RemovePurchaseReturnLineCommand : ICommand<bool>
{
    public Guid PurchaseReturnId { get; init; }

    public Guid PurchaseReturnLineId { get; init; }
}
