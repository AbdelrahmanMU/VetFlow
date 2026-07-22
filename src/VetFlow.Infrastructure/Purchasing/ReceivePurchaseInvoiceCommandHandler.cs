using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.ProductDetails;
using VetFlow.Application.Common;
using VetFlow.Application.Inventory;
using VetFlow.Application.Purchasing.Commands.ReceivePurchaseInvoice;
using VetFlow.Domain.Common;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Receive-purchase-invoice write path (REQ-PUR-005). It loads the invoice with its lines, resolves
/// each product through the <b>sanctioned cross-module read</b> (Catalog <see cref="ProductDetailsQuery"/>
/// — STD-BE-005, ADR-0014 §2) to read its expiry requirement (BR-PUR-013 / DEC-PUR-009) and unit
/// profile, enforces the product-driven required expiry, converts each line's quantity from its
/// purchase unit to the product's canonical stock unit (owner ruling 2026-07-22, using the existing
/// Catalog conversion factors — no new business rule), transitions the aggregate Draft → Received
/// (BR-PUR-009), and stages the inventory effect through the Inventory write kernel
/// (<see cref="IInventoryReceiptWriter"/> — BR-PUR-010, DEC-PUR-008): one batch per line plus the
/// on-hand increase. A single <c>SaveChanges</c> commits the invoice, the batches, and the on-hand
/// rows together, so the whole receipt is atomic (BR-INV-003). Returns <c>null</c> when the invoice
/// does not exist (404); a non-draft invoice (BR-PUR-003 → 409), an empty one (BR-PUR-012 → 409), or
/// a missing required expiry (BR-PUR-013 → 400) is rejected without any effect.
/// </summary>
public sealed class ReceivePurchaseInvoiceCommandHandler(
    VetFlowDbContext dbContext,
    IQueryHandler<ProductDetailsQuery, ProductDetailsDto?> productDetails,
    IInventoryReceiptWriter inventoryWriter,
    TimeProvider timeProvider)
    : ICommandHandler<ReceivePurchaseInvoiceCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(ReceivePurchaseInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.PurchaseInvoices
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == command.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        // Cheap state guards before the per-line cross-module reads (BR-PUR-012). The aggregate
        // re-enforces both in Receive() as the backstop (STD-BE-010).
        if (invoice.Status != PurchaseInvoiceStatus.Draft)
        {
            throw new BusinessRuleException(PurchasingErrorCodes.InvoiceNotDraft);
        }

        if (invoice.Lines.Count == 0)
        {
            throw new BusinessRuleException(PurchasingErrorCodes.InvoiceHasNoLines);
        }

        var receivedAt = timeProvider.GetUtcNow();
        var expiryByLine = command.LineExpiries.ToDictionary(entry => entry.LineId, entry => entry.ExpiryDate);
        var receiptLines = new List<InventoryReceiptLine>(invoice.Lines.Count);

        foreach (var line in invoice.Lines)
        {
            var product = await productDetails.HandleAsync(
                new ProductDetailsQuery { Id = line.ProductId },
                cancellationToken);
            if (product is null)
            {
                throw new BusinessRuleException(
                    PurchasingErrorCodes.LineComposition,
                    new Dictionary<string, string> { ["reason"] = "productNotFound" });
            }

            expiryByLine.TryGetValue(line.Id, out var expiry);

            // Expiry is required exactly when the product definition requires it (DEC-PUR-009); a
            // product that does not require expiry never stores one, even if a date was supplied.
            if (product.HasExpiration && expiry is null)
            {
                throw new BusinessRuleException(
                    PurchasingErrorCodes.ExpiryRequired,
                    new Dictionary<string, string> { ["lineId"] = line.Id.ToString() });
            }

            receiptLines.Add(new InventoryReceiptLine
            {
                ProductId = line.ProductId,
                PurchaseLineId = line.Id,
                StockQuantity = ConvertToStockUnit(product, line.PurchaseUnitId, line.Quantity),
                UnitCost = line.UnitPrice,
                ExpiryDate = product.HasExpiration ? expiry : null,
                ReceivedAt = receivedAt,
            });
        }

        invoice.Receive();
        await inventoryWriter.StageAsync(receiptLines, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return invoice.Id;
    }

    /// <summary>
    /// Convert a quantity from the given purchase unit to the product's canonical stock unit using
    /// the Catalog unit profile's existing conversion factors (BR-CAT-018/019; owner ruling
    /// 2026-07-22 — no new business rule). The factor-to-smallest of a unit is the product of the
    /// chain's <c>QuantityInNextUnit</c> values from that unit down to the smallest; the stock
    /// quantity is <c>quantity × factor(purchaseUnit) ÷ factor(stockUnit)</c>, correct whichever unit
    /// is the larger. Quantities are not money — the EGP rounding policy (BR-PUR-008) does not apply.
    /// </summary>
    private static decimal ConvertToStockUnit(ProductDetailsDto product, Guid purchaseUnitId, decimal quantity)
    {
        var chain = product.Units.OrderBy(unit => unit.Position).ToList();

        var storageIndex = chain.FindIndex(unit => unit.IsStorageUnit);
        if (storageIndex < 0)
        {
            throw new BusinessRuleException(
                PurchasingErrorCodes.LineComposition,
                new Dictionary<string, string> { ["reason"] = "productHasNoStorageUnit" });
        }

        var purchaseIndex = chain.FindIndex(unit => unit.UnitId == purchaseUnitId);
        if (purchaseIndex < 0)
        {
            throw new BusinessRuleException(
                PurchasingErrorCodes.LineComposition,
                new Dictionary<string, string> { ["reason"] = "notPurchaseUnitOfProduct" });
        }

        return quantity * FactorToSmallest(chain, purchaseIndex) / FactorToSmallest(chain, storageIndex);
    }

    /// <summary>Product of the chain's conversion factors from <paramref name="index"/> down to the smallest unit (1 for the smallest).</summary>
    private static decimal FactorToSmallest(IReadOnlyList<ProductUnitDetailsDto> chain, int index)
    {
        var factor = 1m;
        for (var step = index; step < chain.Count - 1; step++)
        {
            factor *= chain[step].QuantityInNextUnit
                ?? throw new BusinessRuleException(
                    PurchasingErrorCodes.LineComposition,
                    new Dictionary<string, string> { ["reason"] = "missingConversionFactor" });
        }

        return factor;
    }
}
