using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.ProductDetails;
using VetFlow.Application.Common;
using VetFlow.Application.Inventory;
using VetFlow.Application.Sales.Commands.CommitSalesInvoice;
using VetFlow.Domain.Common;
using VetFlow.Domain.Inventory;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Catalog;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Commit-sale write path (REQ-SAL-003) — the <b>only</b> event in the system that consumes
/// inventory (BR-SAL-009/010), and the structural mirror of purchase receiving.
///
/// It loads the invoice with its lines, guards the preconditions (draft, at least one line —
/// BR-SAL-009), resolves each product through the <b>sanctioned cross-module read</b> (Catalog
/// <see cref="ProductDetailsQuery"/> — STD-BE-005, ADR-0014 §2), converts every line's quantity
/// from its sale unit into the product's canonical stock unit <b>exactly</b> — an inexact
/// conversion is rejected, never rounded or truncated (BR-INV-058, BR-SAL-012, AC-SAL-013) — and
/// then states the business intent through the Inventory consumption contract
/// (<see cref="IInventoryConsumptionWriter"/> — BR-SAL-010, DEC-SAL-006): "consume N of product P
/// for sale line L".
///
/// <b>Sales never selects a batch and never performs FEFO</b> (BR-SAL-013): batch choice, expiry
/// exclusion, sufficiency, and traceability are Inventory's (DEC-INV-019). What comes back is
/// success or a rejection reason — never a batch list.
///
/// Only when Inventory has staged the effect does the aggregate transition Draft → Committed
/// (BR-SAL-003), and a single <c>SaveChanges</c> commits the status, the batch decrements, the
/// on-hand decreases, and the traceability records together — all or nothing (BR-SAL-010,
/// BR-INV-048). Rejections leave <b>no</b> trace: insufficient saleable stock (BR-INV-052 → 409,
/// naming the products), an inexact conversion (BR-SAL-012 → 400, naming the line), and a
/// concurrent change to an allocated batch (BR-INV-056 → 409, retryable) all keep the invoice a
/// draft with every line intact. Returns <c>null</c> when the invoice does not exist (404).
/// </summary>
public sealed class CommitSalesInvoiceCommandHandler(
    VetFlowDbContext dbContext,
    IQueryHandler<ProductDetailsQuery, ProductDetailsDto?> productDetails,
    IInventoryConsumptionWriter inventoryWriter)
    : ICommandHandler<CommitSalesInvoiceCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(CommitSalesInvoiceCommand command, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.SalesInvoices
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == command.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        // Cheap state guards before the per-product cross-module reads (BR-SAL-009). The aggregate
        // re-enforces both in Commit() as the backstop (STD-BE-010).
        if (invoice.Status != SalesInvoiceStatus.Draft)
        {
            throw new BusinessRuleException(SalesErrorCodes.InvoiceNotDraft);
        }

        if (invoice.Lines.Count == 0)
        {
            throw new BusinessRuleException(SalesErrorCodes.InvoiceHasNoLines);
        }

        // Deterministic line order, so the same invoice always produces the same allocation
        // (BR-INV-050) — the same key the read model orders by.
        var lines = invoice.Lines
            .OrderBy(line => line.AddedAt)
            .ThenBy(line => line.Id)
            .ToList();

        var products = await ResolveProductsAsync(lines, cancellationToken);

        var requests = lines
            .Select(line => new InventoryConsumptionRequest
            {
                ProductId = line.ProductId,
                StockQuantity = ConvertToStockUnit(products[line.ProductId], line),
                // Traceability is part of the request, not an afterthought: every consumed
                // quantity stays attributable to the line that caused it (REQ-INV-008,
                // BR-INV-046/057). It does not make Sales aware of batches (BR-SAL-013).
                SaleLineId = line.Id,
            })
            .ToList();

        var consumption = await inventoryWriter.StageAsync(requests, cancellationToken);
        if (!consumption.Succeeded)
        {
            // Full rejection, nothing staged — not even for the lines that had enough stock
            // (BR-SAL-012, BR-INV-052). The message names the products, using the line's own name
            // snapshot; the amount of detail beyond the name awaits DEC-INV-022.
            throw new BusinessRuleException(
                InventoryErrorCodes.InsufficientStock,
                new Dictionary<string, string> { ["products"] = NameProducts(lines, consumption.InsufficientProductIds) });
        }

        invoice.Commit();

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // An allocated batch changed between allocation and commit (BR-INV-056, DEC-INV-023):
            // the sale fails and asks for a retry rather than overwriting inventory silently.
            // Nothing is committed — the invoice stays a draft with all its lines, and retrying
            // reallocates against the new state. The scope is the batch: a concurrent sale on a
            // different batch of the same product does not land here (R6).
            throw new BusinessRuleException(InventoryErrorCodes.ConcurrencyConflict);
        }

        return invoice.Id;
    }

    /// <summary>Resolve each distinct product once through the sanctioned Catalog read path (STD-BE-005).</summary>
    private async Task<Dictionary<Guid, ProductDetailsDto>> ResolveProductsAsync(
        IEnumerable<SalesLineItem> lines,
        CancellationToken cancellationToken)
    {
        var products = new Dictionary<Guid, ProductDetailsDto>();

        foreach (var productId in lines.Select(line => line.ProductId).Distinct())
        {
            var product = await productDetails.HandleAsync(
                new ProductDetailsQuery { Id = productId },
                cancellationToken);

            products[productId] = product
                ?? throw new BusinessRuleException(
                    SalesErrorCodes.LineComposition,
                    new Dictionary<string, string> { ["reason"] = "productNotFound" });
        }

        return products;
    }

    /// <summary>
    /// Convert a line's quantity from its sale unit into the product's canonical stock unit through
    /// the shared Catalog conversion (BR-CAT-018/019 — an existing capability, no new rule), and
    /// enforce that the conversion is <b>exact</b>: the stock unit is the smallest measurable unit
    /// (BR-CAT-020 as amended by DEC-CAT-033), so a residue can only come from a product configured
    /// against that rule. Such a line is rejected — the quantity is never rounded and never
    /// truncated, because rounding would break the BR-INV-005 invariant silently, with no
    /// adjustment path to correct it (BR-INV-058, BR-SAL-012).
    /// </summary>
    private static decimal ConvertToStockUnit(ProductDetailsDto product, SalesLineItem line)
    {
        var failure = ProductUnitConversion.TryConvertToStockUnit(
            product,
            line.SaleUnitId,
            line.Quantity,
            out var stockQuantity,
            out var isExact);

        if (failure is not UnitConversionFailure.None)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.InexactUnitConversion,
                new Dictionary<string, string>
                {
                    ["reason"] = failure switch
                    {
                        UnitConversionFailure.NoStorageUnit => "productHasNoStorageUnit",
                        UnitConversionFailure.UnitNotInProfile => "notSaleUnitOfProduct",
                        _ => "missingConversionFactor",
                    },
                    ["product"] = line.ProductName,
                });
        }

        if (!isExact)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.InexactUnitConversion,
                new Dictionary<string, string>
                {
                    ["reason"] = "conversionNotExact",
                    ["product"] = line.ProductName,
                });
        }

        return stockQuantity;
    }

    /// <summary>
    /// Name the products that fell short, from the lines' own immutable name snapshots
    /// (BR-SAL-006) — no extra catalog read, and no dependence on Inventory knowing catalog names.
    /// </summary>
    private static string NameProducts(IEnumerable<SalesLineItem> lines, IReadOnlyCollection<Guid> productIds) =>
        string.Join(
            "، ",
            lines.Where(line => productIds.Contains(line.ProductId))
                .Select(line => line.ProductName)
                .Distinct());
}
