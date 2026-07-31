using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Commands.AddPurchaseReturnLine;
using VetFlow.Domain.Common;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Add-purchase-return-line write path (REQ-PUR-006, BR-PUR-016/017, AC-PUR-021/022).
///
/// <para>It does the two things the aggregate cannot do for itself, because both need data outside
/// it:</para>
/// <list type="number">
///   <item><description><b>Resolves the batch</b> (BR-PUR-017). The destination is the batch the
///   original purchase line created — one line, one batch (DEC-PUR-008) — looked up by
///   <c>PurchaseLineId</c>. It is never chosen, and FEFO is never consulted: a return puts stock
///   back where it came from (BR-INV-069).</description></item>
///   <item><description><b>Enforces the returnable ceiling</b> (BR-PUR-016), derived from the
///   committed returns of this invoice. Partial returns are allowed by the owner's ruling
///   (DEC-PUR-010), so the check is against the remainder, not the whole line.</description></item>
/// </list>
///
/// <para>Returns <c>null</c> when the return, the original line, or its batch does not exist (404).
/// A committed return rejects with VTF-PUR-018 (BR-PUR-018 → 409) and an over-return with
/// VTF-PUR-016 (→ 409); neither mutates anything.</para>
/// </summary>
public sealed class AddPurchaseReturnLineCommandHandler(VetFlowDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<AddPurchaseReturnLineCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(AddPurchaseReturnLineCommand command, CancellationToken cancellationToken)
    {
        var purchaseReturn = await dbContext.PurchaseReturns
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == command.PurchaseReturnId, cancellationToken);

        if (purchaseReturn is null)
        {
            return null;
        }

        // Cheap state guard before any further read; the aggregate re-enforces it (STD-BE-010).
        if (purchaseReturn.Status != PurchaseReturnStatus.Draft)
        {
            throw new BusinessRuleException(PurchasingErrorCodes.ReturnNotDraft);
        }

        var purchaseLineItemId = command.PurchaseLineItemId!.Value;
        var quantity = command.Quantity!.Value;

        // The original line must belong to *this return's* invoice — one original invoice per
        // return (BR-PUR-015, DEC-PUR-010). Without this join a caller could return a line of some
        // other invoice through this document, which is exactly what the one-invoice rule forbids.
        // Queried through the line DbSet and its shadow FK rather than by walking the invoice's
        // Lines navigation: that collection is encapsulated (AsReadOnly over a private field),
        // which the provider cannot translate into a join.
        var originalLine = await dbContext.PurchaseLineItems
            .FirstOrDefaultAsync(
                line => line.Id == purchaseLineItemId
                    && EF.Property<Guid>(line, "PurchaseInvoiceId") == purchaseReturn.PurchaseInvoiceId,
                cancellationToken);

        if (originalLine is null)
        {
            return null;
        }

        var batch = await dbContext.InventoryBatches
            .FirstOrDefaultAsync(item => item.PurchaseLineId == purchaseLineItemId, cancellationToken);

        if (batch is null)
        {
            return null;
        }

        var alreadyReturned = await PurchaseReturnableQuantities.GetAlreadyReturnedAsync(
            dbContext, purchaseReturn.PurchaseInvoiceId, cancellationToken);
        alreadyReturned.TryGetValue(purchaseLineItemId, out var returnedSoFar);

        // Lines already on *this* draft count too: three additions of 4 against a remainder of 10
        // must fail on the third, not silently pass because each one alone fits.
        var onThisDraft = purchaseReturn.Lines
            .Where(line => line.PurchaseLineItemId == purchaseLineItemId)
            .Sum(line => line.Quantity);

        var remaining = originalLine.Quantity - returnedSoFar - onThisDraft;

        if (quantity > remaining)
        {
            throw new BusinessRuleException(
                PurchasingErrorCodes.ReturnQuantityExceedsReturnable,
                new Dictionary<string, string>
                {
                    ["requested"] = quantity.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
                    ["remaining"] = remaining.ToString("G", System.Globalization.CultureInfo.InvariantCulture),
                });
        }

        var line = purchaseReturn.AddLine(
            Guid.NewGuid(),
            purchaseLineItemId,
            originalLine.ProductId,
            originalLine.ProductName,
            batch.Id,
            quantity,
            timeProvider.GetUtcNow());

        await dbContext.SaveChangesAsync(cancellationToken);
        return line.Id;
    }
}
