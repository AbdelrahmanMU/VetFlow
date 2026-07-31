using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.ProductDetails;
using VetFlow.Application.Common;
using VetFlow.Application.Sales.Commands.AddSalesReturnLine;
using VetFlow.Domain.Common;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Add-sales-return-line write path (REQ-SAL-004, BR-SAL-016, AC-SAL-016/017).
///
/// <para>It does the three things the aggregate cannot do for itself, because each needs data
/// outside it:</para>
/// <list type="number">
///   <item><description><b>Binds the line to this return's own invoice</b> — one original invoice
///   per return (BR-SAL-015, DEC-SAL-010). Without that join a caller could return a line of some
///   other invoice through this document.</description></item>
///   <item><description><b>Enforces the returnable ceiling</b> (BR-SAL-016), derived from the
///   committed returns of this invoice. Partial returns are allowed by the owner's ruling
///   (DEC-SAL-010), so the check is against the remainder, not the whole line.</description></item>
///   <item><description><b>Resolves the product's splittability</b> through the sanctioned Catalog
///   read (STD-BE-005) so the aggregate can enforce BR-SAL-016's last clause: a partial return
///   respects <c>IsSplittable</c> exactly as the sale did (DEC-SAL-007).</description></item>
/// </list>
///
/// <para><b>No batch is resolved here at all</b>, unlike the purchase-return handler: one sale line
/// may have left through several batches, the destination is read from the consumption trace at
/// commit (BR-SAL-017), and Sales may not hold a batch reference (BR-SAL-013).</para>
///
/// <para>Returns <c>null</c> when the return or the original sale line does not exist (404). A
/// committed return rejects with VTF-SAL-018 (BR-SAL-018 → 409) and an over-return with VTF-SAL-016
/// (→ 409); neither mutates anything.</para>
/// </summary>
public sealed class AddSalesReturnLineCommandHandler(
    VetFlowDbContext dbContext,
    IQueryHandler<ProductDetailsQuery, ProductDetailsDto?> productDetails,
    TimeProvider timeProvider)
    : ICommandHandler<AddSalesReturnLineCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(AddSalesReturnLineCommand command, CancellationToken cancellationToken)
    {
        var salesReturn = await dbContext.SalesReturns
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == command.SalesReturnId, cancellationToken);

        if (salesReturn is null)
        {
            return null;
        }

        // Cheap state guard before any further read; the aggregate re-enforces it (STD-BE-010).
        if (salesReturn.Status != SalesReturnStatus.Draft)
        {
            throw new BusinessRuleException(SalesErrorCodes.ReturnNotDraft);
        }

        var salesLineItemId = command.SalesLineItemId!.Value;
        var quantity = command.Quantity!.Value;

        // The original line must belong to *this return's* invoice — one original invoice per return
        // (BR-SAL-015, DEC-SAL-010). Queried through the line DbSet and its shadow FK rather than by
        // walking the invoice's Lines navigation: that collection is encapsulated (AsReadOnly over a
        // private field), which the provider cannot translate into a join.
        var originalLine = await dbContext.SalesLineItems
            .FirstOrDefaultAsync(
                line => line.Id == salesLineItemId
                    && EF.Property<Guid>(line, "SalesInvoiceId") == salesReturn.SalesInvoiceId,
                cancellationToken);

        if (originalLine is null)
        {
            return null;
        }

        var alreadyReturned = await SalesReturnableQuantities.GetAlreadyReturnedAsync(
            dbContext, salesReturn.SalesInvoiceId, cancellationToken);
        alreadyReturned.TryGetValue(salesLineItemId, out var returnedSoFar);

        // Lines already on *this* draft count too: three additions of 4 against a remainder of 10
        // must fail on the third, not silently pass because each one alone fits.
        var onThisDraft = salesReturn.Lines
            .Where(line => line.SalesLineItemId == salesLineItemId)
            .Sum(line => line.Quantity);

        var remaining = originalLine.Quantity - returnedSoFar - onThisDraft;

        if (quantity > remaining)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.ReturnQuantityExceedsReturnable,
                new Dictionary<string, string>
                {
                    ["requested"] = quantity.ToString("G", CultureInfo.InvariantCulture),
                    ["remaining"] = remaining.ToString("G", CultureInfo.InvariantCulture),
                });
        }

        // The splittability capability comes from the catalog through the sanctioned read path
        // (STD-BE-005), exactly as adding a *sale* line does — the same product, the same rule
        // (DEC-SAL-007, BR-SAL-016). A product that has since disappeared is a malformed line, not a
        // silent pass.
        var product = await productDetails.HandleAsync(
            new ProductDetailsQuery { Id = originalLine.ProductId },
            cancellationToken);

        if (product is null)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.ReturnLineComposition,
                new Dictionary<string, string> { ["reason"] = "productNotFound" });
        }

        var line = salesReturn.AddLine(
            Guid.NewGuid(),
            salesLineItemId,
            originalLine.ProductId,
            originalLine.ProductName,
            quantity,
            product.IsSplittable,
            timeProvider.GetUtcNow());

        await dbContext.SaveChangesAsync(cancellationToken);
        return line.Id;
    }
}
