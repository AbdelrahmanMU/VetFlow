using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Sales.Queries.SalesDetails;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Sales-details read (REQ-SAL-002). Projects straight to the response DTO — a deliberate CQRS-lite
/// read that bypasses the domain (ADR-0014 §5), mirroring the purchase-details handler. Returns
/// <c>null</c> when the invoice does not exist so the endpoint can answer 404 (AC-SAL-006).
/// </summary>
public sealed class SalesDetailsQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<SalesDetailsQuery, SalesDetailsDto?>
{
    public async Task<SalesDetailsDto?> HandleAsync(SalesDetailsQuery query, CancellationToken cancellationToken)
    {
        return await dbContext.SalesInvoices
            .AsNoTracking()
            .Where(invoice => invoice.Id == query.Id)
            .Select(invoice => new SalesDetailsDto
            {
                Id = invoice.Id,
                Number = invoice.Number,
                CustomerName = invoice.CustomerName,
                SaleDate = invoice.SaleDate,
                Status = invoice.Status == SalesInvoiceStatus.Committed
                    ? SalesInvoiceStatusDto.Committed
                    : SalesInvoiceStatusDto.Draft,
                Total = new MoneyDto { Amount = invoice.TotalAmount, Currency = Currencies.EgyptianPound },
                Notes = invoice.Notes,
                CreatedAt = invoice.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
