using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Queries.PurchaseReturnableLines;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Read path behind the new-purchase-return screen (REQ-PUR-006, BR-PUR-016). Returns <c>null</c>
/// when the invoice does not exist <b>or is not Received</b> (⇒ 404): a screen that cannot legally
/// produce a return should not render a table of lines the command would then reject (BR-PUR-015).
///
/// <para>Two queries, both constant in the number of lines: the invoice with its lines, and the
/// already-returned sums grouped in the database. The returnable remainder is computed from the
/// <b>same</b> helper the add-line command uses, so the screen can never promise a quantity the
/// write path refuses.</para>
/// </summary>
public sealed class PurchaseReturnableLinesQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<PurchaseReturnableLinesQuery, IReadOnlyList<PurchaseReturnableLineDto>?>
{
    public async Task<IReadOnlyList<PurchaseReturnableLineDto>?> HandleAsync(
        PurchaseReturnableLinesQuery query,
        CancellationToken cancellationToken)
    {
        var invoice = await dbContext.PurchaseInvoices
            .AsNoTracking()
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == query.PurchaseInvoiceId, cancellationToken);

        if (invoice is null || invoice.Status != PurchaseInvoiceStatus.Received)
        {
            return null;
        }

        var alreadyReturned = await PurchaseReturnableQuantities.GetAlreadyReturnedAsync(
            dbContext, invoice.Id, cancellationToken);

        return invoice.Lines
            .Select(line =>
            {
                alreadyReturned.TryGetValue(line.Id, out var returned);
                return new PurchaseReturnableLineDto
                {
                    PurchaseLineItemId = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    PurchaseUnitName = line.PurchaseUnitName,
                    Quantity = line.Quantity,
                    ReturnedQuantity = returned,
                    ReturnableQuantity = line.Quantity - returned,
                };
            })
            .ToList();
    }
}
