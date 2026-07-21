using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.ProductDetails;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Commands.AddPurchaseLineItem;
using VetFlow.Domain.Common;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Add-purchase-line-item write path (REQ-PUR-004, BR-PUR-005). It loads the invoice
/// with its lines, resolves the product and its chosen purchase unit through the
/// <b>sanctioned cross-module read path</b> — the Catalog <see cref="ProductDetailsQuery"/>
/// handler (STD-BE-005, ADR-0014 §2), never Catalog internals — captures the name
/// snapshot (BR-PUR-007), and lets the aggregate add the line and recompute the total
/// (BR-PUR-006, DEC-PUR-003) in a single <c>SaveChanges</c> (STD-BE-024). Returns
/// <c>null</c> when the invoice does not exist (404); the aggregate rejects a non-draft
/// invoice (BR-PUR-003 → 409) and a malformed line (BR-PUR-005 → 400).
/// </summary>
public sealed class AddPurchaseLineItemCommandHandler(
    VetFlowDbContext dbContext,
    IQueryHandler<ProductDetailsQuery, ProductDetailsDto?> productDetails,
    TimeProvider timeProvider)
    : ICommandHandler<AddPurchaseLineItemCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(AddPurchaseLineItemCommand command, CancellationToken cancellationToken)
    {
        var invoice = await dbContext.PurchaseInvoices
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
                PurchasingErrorCodes.LineComposition,
                new Dictionary<string, string> { ["reason"] = "productNotFound" });
        }

        // The unit must be a purchase unit of the selected product (TS-PUR-020); the
        // picker enforces this in the UI, the handler enforces it as the backstop.
        var unit = product.Units.FirstOrDefault(
            candidate => candidate.UnitId == command.PurchaseUnitId && candidate.IsPurchaseUnit);
        if (unit is null)
        {
            throw new BusinessRuleException(
                PurchasingErrorCodes.LineComposition,
                new Dictionary<string, string> { ["reason"] = "notPurchaseUnitOfProduct" });
        }

        var line = invoice.AddLine(
            Guid.NewGuid(),
            product.Id,
            product.ArabicName,
            unit.UnitId,
            unit.UnitName,
            command.Quantity,
            command.UnitPrice,
            timeProvider.GetUtcNow());

        await dbContext.SaveChangesAsync(cancellationToken);
        return line.Id;
    }
}
