using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Commands.CommitSalesInvoice;

/// <summary>
/// Commit a draft sales invoice (REQ-SAL-003) — the single event that consumes inventory
/// (BR-SAL-009/010). The command carries nothing but the invoice: quantities, units, and prices are
/// already on its lines, and <b>batch selection belongs to Inventory alone</b> (BR-SAL-013,
/// DEC-SAL-006) — there is deliberately no batch, no allocation hint, and no override here.
/// Returns <c>null</c> when the invoice does not exist (404).
/// </summary>
public sealed record CommitSalesInvoiceCommand : ICommand<Guid?>
{
    public required Guid InvoiceId { get; init; }
}
