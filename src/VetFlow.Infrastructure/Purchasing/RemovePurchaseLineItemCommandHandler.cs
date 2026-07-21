using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Commands.RemovePurchaseLineItem;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Remove-purchase-line-item write path (REQ-PUR-004, BR-PUR-005). It loads the invoice
/// with its lines and lets the aggregate remove the line and recompute the total
/// (BR-PUR-006, DEC-PUR-003) in a single <c>SaveChanges</c> (STD-BE-024). Returns
/// <c>null</c> when the invoice or the line does not exist (404); the aggregate rejects
/// a non-draft invoice (BR-PUR-003 → 409).
/// </summary>
public sealed class RemovePurchaseLineItemCommandHandler(VetFlowDbContext dbContext)
    : ICommandHandler<RemovePurchaseLineItemCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(RemovePurchaseLineItemCommand command, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.PurchaseInvoices
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == command.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        if (!invoice.RemoveLine(command.LineId))
        {
            return null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return command.LineId;
    }
}
