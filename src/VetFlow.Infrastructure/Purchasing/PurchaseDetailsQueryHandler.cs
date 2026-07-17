using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Queries.PurchaseDetails;
using VetFlow.Application.Purchasing.Queries.PurchaseList;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Purchase-details read (REQ-PUR-002). Projects straight to the response DTO — a
/// deliberate CQRS-lite read that bypasses the domain (ADR-0014 §5), mirroring the
/// Catalog product-details handler. Returns <c>null</c> when the invoice does not
/// exist so the endpoint can answer 404 (AC-PUR-005).
/// </summary>
public sealed class PurchaseDetailsQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<PurchaseDetailsQuery, PurchaseDetailsDto?>
{
    public async Task<PurchaseDetailsDto?> HandleAsync(
        PurchaseDetailsQuery query,
        CancellationToken cancellationToken)
    {
        return await dbContext.PurchaseInvoices
            .AsNoTracking()
            .Where(invoice => invoice.Id == query.Id)
            .Select(invoice => new PurchaseDetailsDto
            {
                Id = invoice.Id,
                Number = invoice.Number,
                SupplierName = invoice.SupplierName,
                SupplierInvoiceReference = invoice.SupplierInvoiceReference,
                InvoiceDate = invoice.InvoiceDate,
                Status = invoice.Status == PurchaseInvoiceStatus.Received
                    ? PurchaseInvoiceStatusDto.Received
                    : invoice.Status == PurchaseInvoiceStatus.Cancelled
                        ? PurchaseInvoiceStatusDto.Cancelled
                        : PurchaseInvoiceStatusDto.Draft,
                Total = new MoneyDto { Amount = invoice.TotalAmount, Currency = Currencies.EgyptianPound },
                Notes = invoice.Notes,
                CreatedAt = invoice.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
