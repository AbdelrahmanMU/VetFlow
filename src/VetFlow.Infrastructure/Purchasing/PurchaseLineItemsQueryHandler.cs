using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.ProductDetails;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Queries.PurchaseLineItems;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Purchase-line-items read (REQ-PUR-004). Projects the invoice's lines to the response DTO — a
/// CQRS-lite read that bypasses the domain (ADR-0014 §5), ordered by add time for a deterministic
/// list. Each line also carries <c>RequiresExpiry</c>, a <b>live</b> read of whether the line's
/// product currently requires an expiry date (BR-PUR-013 / DEC-PUR-009), resolved through the
/// sanctioned cross-module read (Catalog <see cref="ProductDetailsQuery"/> — STD-BE-005) once per
/// distinct product so the receive dialog can mark the field required. Returns <c>null</c> when the
/// invoice does not exist so the endpoint answers 404; an existing invoice with no lines returns an
/// empty list (BR-PUR-005).
/// </summary>
public sealed class PurchaseLineItemsQueryHandler(
    VetFlowDbContext dbContext,
    IQueryHandler<ProductDetailsQuery, ProductDetailsDto?> productDetails)
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

        var lines = await dbContext.PurchaseInvoices
            .AsNoTracking()
            .Where(invoice => invoice.Id == query.InvoiceId)
            .SelectMany(invoice => invoice.Lines)
            .OrderBy(line => line.AddedAt)
            .ThenBy(line => line.Id)
            .Select(line => new LineProjection(
                line.Id,
                line.ProductId,
                line.ProductName,
                line.PurchaseUnitId,
                line.PurchaseUnitName,
                line.Quantity,
                line.UnitPrice,
                line.LineTotal))
            .ToListAsync(cancellationToken);

        // Resolve the current expiry requirement once per distinct product (DEC-PUR-009) via the
        // sanctioned read path; a purchase invoice references only a handful of products.
        var requiresExpiryByProduct = new Dictionary<Guid, bool>();
        foreach (var productId in lines.Select(line => line.ProductId).Distinct())
        {
            var product = await productDetails.HandleAsync(new ProductDetailsQuery { Id = productId }, cancellationToken);
            requiresExpiryByProduct[productId] = product?.HasExpiration ?? false;
        }

        return lines
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
                RequiresExpiry = requiresExpiryByProduct[line.ProductId],
            })
            .ToList();
    }

    private sealed record LineProjection(
        Guid Id,
        Guid ProductId,
        string ProductName,
        Guid PurchaseUnitId,
        string PurchaseUnitName,
        decimal Quantity,
        decimal UnitPrice,
        decimal LineTotal);
}
