using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.ProductDetails;
using VetFlow.Application.Common;
using VetFlow.Application.Sales.Commands.AddSalesLineItem;
using VetFlow.Domain.Common;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Add-sales-line-item write path (REQ-SAL-001, BR-SAL-004). It loads the invoice with its lines,
/// resolves the product and its chosen sale unit through the <b>sanctioned cross-module read
/// path</b> — the Catalog <see cref="ProductDetailsQuery"/> handler (STD-BE-005, ADR-0014 §2),
/// never Catalog internals — and captures the three snapshots the line keeps forever (BR-SAL-006):
/// the product name, the sale-unit name, and the <b>catalog sale price</b>. The price is a snapshot
/// only: it is neither sent by the client nor editable (DEC-SAL-003 — no discount, no override, no
/// reason, no audit).
///
/// Three rejections happen here, each without mutation: the unit is not a <b>sale</b> unit of the
/// product (BR-SAL-004, TS-SAL-003), no sale price is defined for that unit (TS-SAL-006 — a price
/// is never invented, and never defaulted to zero), and the product does not exist. The
/// splittability constraint (DEC-SAL-007) is enforced by the aggregate, which is given the
/// product's <c>IsSplittable</c> capability. The aggregate also recomputes the total (BR-SAL-005)
/// in a single <c>SaveChanges</c> (STD-BE-024). Returns <c>null</c> when the invoice does not exist
/// (404); a committed invoice is rejected (BR-SAL-011 → 409).
/// </summary>
public sealed class AddSalesLineItemCommandHandler(
    VetFlowDbContext dbContext,
    IQueryHandler<ProductDetailsQuery, ProductDetailsDto?> productDetails,
    TimeProvider timeProvider)
    : ICommandHandler<AddSalesLineItemCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(AddSalesLineItemCommand command, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.SalesInvoices
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == command.InvoiceId, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        var product = await productDetails.HandleAsync(
            new ProductDetailsQuery { Id = command.ProductId },
            cancellationToken);
        if (product is null)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.LineComposition,
                new Dictionary<string, string> { ["reason"] = "productNotFound" });
        }

        // The unit must be a sale unit of the selected product (BR-SAL-004); the picker enforces
        // this in the UI, the handler enforces it as the backstop — the purchase-unit precedent.
        var unit = product.Units.FirstOrDefault(
            candidate => candidate.UnitId == command.SaleUnitId && candidate.IsSaleUnit);
        if (unit is null)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.LineComposition,
                new Dictionary<string, string> { ["reason"] = "notSaleUnitOfProduct" });
        }

        // A line whose unit has no catalog sale price is rejected — no price is invented and zero
        // is not a substitute (BR-SAL-004, TS-SAL-006).
        if (unit.SellingPrice is not { } sellingPrice)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.LineComposition,
                new Dictionary<string, string> { ["reason"] = "noSalePriceForUnit" });
        }

        var line = invoice.AddLine(
            Guid.NewGuid(),
            product.Id,
            product.ArabicName,
            unit.UnitId,
            unit.UnitName,
            command.Quantity,
            sellingPrice.Amount,
            product.IsSplittable,
            timeProvider.GetUtcNow());

        await dbContext.SaveChangesAsync(cancellationToken);
        return line.Id;
    }
}
