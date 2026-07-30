using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Sales.Commands.RemoveSalesLineItem;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Remove-sales-line-item write path (REQ-SAL-001, BR-SAL-004). It loads the invoice with its lines
/// and lets the aggregate remove the line and recompute the total (BR-SAL-005) in a single
/// <c>SaveChanges</c> (STD-BE-024). Returns <c>null</c> when the invoice or the line does not exist
/// (404); the aggregate rejects a committed invoice (BR-SAL-011 → 409). Removing a draft line has
/// no inventory effect — a draft never held any (BR-SAL-010).
/// </summary>
public sealed class RemoveSalesLineItemCommandHandler(VetFlowDbContext dbContext)
    : ICommandHandler<RemoveSalesLineItemCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(RemoveSalesLineItemCommand command, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.SalesInvoices
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
