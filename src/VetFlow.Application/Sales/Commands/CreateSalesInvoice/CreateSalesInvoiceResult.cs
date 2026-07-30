namespace VetFlow.Application.Sales.Commands.CreateSalesInvoice;

/// <summary>The created invoice's identity and its generated system number (BR-SAL-002).</summary>
public sealed record CreateSalesInvoiceResult
{
    public required Guid Id { get; init; }

    public required string Number { get; init; }
}
