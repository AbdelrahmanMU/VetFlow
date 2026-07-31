using VetFlow.Application.Sales.Commands.AddSalesReturnLine;

namespace VetFlow.Api.Endpoints.Sales;

/// <summary>
/// JSON body of POST /api/v1/sales-returns/{id}/lines (camelCase, STD-API-032).
///
/// <para><b>No batch field.</b> A sale line may have left through several batches, the destinations
/// are read from the recorded consumption trace at commit (BR-SAL-017, BR-INV-069), and Sales may
/// hold no batch reference at all (BR-SAL-013). Accepting one over the wire would expose a choice
/// the rules forbid and let a caller push stock into a batch the goods never left.</para>
/// </summary>
public sealed record AddSalesReturnLineRequest
{
    public Guid? SalesLineItemId { get; init; }

    public decimal? Quantity { get; init; }

    public AddSalesReturnLineCommand ToCommand(Guid salesReturnId) => new()
    {
        SalesReturnId = salesReturnId,
        SalesLineItemId = SalesLineItemId,
        Quantity = Quantity,
    };
}
