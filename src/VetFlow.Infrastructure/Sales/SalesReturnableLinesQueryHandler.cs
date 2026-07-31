using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Sales.Queries.SalesReturnableLines;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Read path behind the new-sales-return screen (REQ-SAL-004, BR-SAL-016). Returns <c>null</c> when
/// the invoice does not exist <b>or is not Committed</b> (⇒ 404): a screen that cannot legally
/// produce a return should not render a table of lines the command would then reject (BR-SAL-015,
/// AC-SAL-015).
///
/// <para>Two queries, both constant in the number of lines: the invoice with its lines, and the
/// already-returned sums grouped in the database. The returnable remainder is computed from the
/// <b>same</b> helper the add-line command uses, so the screen can never promise a quantity the
/// write path refuses.</para>
///
/// <para><b>No price is projected</b>, though the sale lines carry one: a return has no financial
/// effect (DEC-INV-035) and the screen has no amount column (ui.md).</para>
/// </summary>
public sealed class SalesReturnableLinesQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<SalesReturnableLinesQuery, IReadOnlyList<SalesReturnableLineDto>?>
{
    public async Task<IReadOnlyList<SalesReturnableLineDto>?> HandleAsync(
        SalesReturnableLinesQuery query,
        CancellationToken cancellationToken)
    {
        var invoice = await dbContext.SalesInvoices
            .AsNoTracking()
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == query.SalesInvoiceId, cancellationToken);

        if (invoice is null || invoice.Status != SalesInvoiceStatus.Committed)
        {
            return null;
        }

        var alreadyReturned = await SalesReturnableQuantities.GetAlreadyReturnedAsync(
            dbContext, invoice.Id, cancellationToken);

        return invoice.Lines
            .OrderBy(line => line.AddedAt)
            .ThenBy(line => line.Id)
            .Select(line =>
            {
                alreadyReturned.TryGetValue(line.Id, out var returned);
                return new SalesReturnableLineDto
                {
                    SalesLineItemId = line.Id,
                    ProductId = line.ProductId,
                    ProductName = line.ProductName,
                    SaleUnitName = line.SaleUnitName,
                    Quantity = line.Quantity,
                    ReturnedQuantity = returned,
                    ReturnableQuantity = line.Quantity - returned,
                };
            })
            .ToList();
    }
}
