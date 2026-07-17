namespace VetFlow.Application.Purchasing.Commands.CreatePurchaseInvoice;

/// <summary>
/// The lightweight result of creating a purchase invoice (STD-BE-021 — never a
/// read DTO): the new id and the system-generated number (BR-PUR-002).
/// </summary>
public sealed record CreatePurchaseInvoiceResult
{
    public required Guid Id { get; init; }

    public required string Number { get; init; }
}
