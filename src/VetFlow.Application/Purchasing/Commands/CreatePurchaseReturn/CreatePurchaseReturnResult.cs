namespace VetFlow.Application.Purchasing.Commands.CreatePurchaseReturn;

/// <summary>
/// The lightweight result of creating a purchase return (STD-BE-021 — never a read DTO): the new
/// id and the system-generated <c>PRT-</c> number (BR-PUR-014).
/// </summary>
public sealed record CreatePurchaseReturnResult
{
    public required Guid Id { get; init; }

    public required string Number { get; init; }
}
