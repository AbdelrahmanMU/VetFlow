namespace VetFlow.Application.Sales.Commands.CreateSalesReturn;

/// <summary>
/// The lightweight result of creating a sales return (STD-BE-021 — never a read DTO): the new id
/// and the system-generated <c>SRT-</c> number (BR-SAL-014).
/// </summary>
public sealed record CreateSalesReturnResult
{
    public required Guid Id { get; init; }

    public required string Number { get; init; }
}
