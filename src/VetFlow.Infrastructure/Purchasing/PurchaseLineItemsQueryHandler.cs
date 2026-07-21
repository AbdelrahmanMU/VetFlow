using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Queries.PurchaseLineItems;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Purchase-line-items read (REQ-PUR-004). Projects the invoice's lines straight to the
/// response DTO — a CQRS-lite read that bypasses the domain (ADR-0014 §5), ordered by add
/// time for a deterministic list. Returns <c>null</c> when the invoice does not exist so
/// the endpoint answers 404; an existing invoice with no lines returns an empty list
/// (BR-PUR-005).
/// </summary>
public sealed class PurchaseLineItemsQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<PurchaseLineItemsQuery, IReadOnlyList<PurchaseLineItemDto>?>
{
    public async Task<IReadOnlyList<PurchaseLineItemDto>?> HandleAsync(
        PurchaseLineItemsQuery query,
        CancellationToken cancellationToken)
    {
        var invoiceExists = await dbContext.PurchaseInvoices
            .AnyAsync(invoice => invoice.Id == query.InvoiceId, cancellationToken);
        if (!invoiceExists)
        {
            return null;
        }

        return await dbContext.PurchaseInvoices
            .AsNoTracking()
            .Where(invoice => invoice.Id == query.InvoiceId)
            .SelectMany(invoice => invoice.Lines)
            .OrderBy(line => line.AddedAt)
            .ThenBy(line => line.Id)
            .Select(line => new PurchaseLineItemDto
            {
                Id = line.Id,
                ProductId = line.ProductId,
                ProductName = line.ProductName,
                PurchaseUnitId = line.PurchaseUnitId,
                PurchaseUnitName = line.PurchaseUnitName,
                Quantity = line.Quantity,
                UnitPrice = new MoneyDto { Amount = line.UnitPrice, Currency = Currencies.EgyptianPound },
                LineTotal = new MoneyDto { Amount = line.LineTotal, Currency = Currencies.EgyptianPound },
            })
            .ToListAsync(cancellationToken);
    }
}
