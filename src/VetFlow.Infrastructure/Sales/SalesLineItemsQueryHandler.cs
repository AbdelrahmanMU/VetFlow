using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Sales.Queries.SalesLineItems;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Sales-line-items read (REQ-SAL-002). Projects the invoice's lines to the response DTO — a
/// CQRS-lite read that bypasses the domain (ADR-0014 §5), ordered by add time then id for a
/// deterministic list, in one query with no per-row lookups. Returns <c>null</c> when the invoice
/// does not exist so the endpoint answers 404; an existing invoice with no lines returns an empty
/// list (BR-SAL-004). It exposes <b>no batch information</b> (BR-SAL-013).
/// </summary>
public sealed class SalesLineItemsQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<SalesLineItemsQuery, IReadOnlyList<SalesLineItemDto>?>
{
    public async Task<IReadOnlyList<SalesLineItemDto>?> HandleAsync(
        SalesLineItemsQuery query,
        CancellationToken cancellationToken)
    {
        var invoiceExists = await dbContext.SalesInvoices
            .AnyAsync(invoice => invoice.Id == query.InvoiceId, cancellationToken);
        if (!invoiceExists)
        {
            return null;
        }

        return await dbContext.SalesInvoices
            .AsNoTracking()
            .Where(invoice => invoice.Id == query.InvoiceId)
            .SelectMany(invoice => invoice.Lines)
            .OrderBy(line => line.AddedAt)
            .ThenBy(line => line.Id)
            .Select(line => new SalesLineItemDto
            {
                Id = line.Id,
                ProductId = line.ProductId,
                ProductName = line.ProductName,
                SaleUnitId = line.SaleUnitId,
                SaleUnitName = line.SaleUnitName,
                Quantity = line.Quantity,
                UnitPrice = new MoneyDto { Amount = line.UnitPrice, Currency = Currencies.EgyptianPound },
                LineTotal = new MoneyDto { Amount = line.LineTotal, Currency = Currencies.EgyptianPound },
            })
            .ToListAsync(cancellationToken);
    }
}
