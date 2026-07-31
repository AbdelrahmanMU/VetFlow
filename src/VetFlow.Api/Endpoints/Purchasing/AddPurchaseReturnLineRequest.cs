using VetFlow.Application.Purchasing.Commands.AddPurchaseReturnLine;

namespace VetFlow.Api.Endpoints.Purchasing;

/// <summary>
/// JSON body of POST /api/v1/purchase-returns/{id}/lines (camelCase, STD-API-032).
///
/// <para><b>No batch field.</b> The destination batch is derived from the original purchase line
/// (BR-PUR-017, DEC-PUR-008); accepting one over the wire would expose a choice the rules forbid
/// and let a caller push stock into the wrong batch.</para>
/// </summary>
public sealed record AddPurchaseReturnLineRequest
{
    public Guid? PurchaseLineItemId { get; init; }

    public decimal? Quantity { get; init; }

    public AddPurchaseReturnLineCommand ToCommand(Guid purchaseReturnId) => new()
    {
        PurchaseReturnId = purchaseReturnId,
        PurchaseLineItemId = PurchaseLineItemId,
        Quantity = Quantity,
    };
}
