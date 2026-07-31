using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Commands.CreatePurchaseReturn;
using VetFlow.Domain.Common;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Create-purchase-return write path (REQ-PUR-006, AC-PUR-019). It allocates the <c>PRT-</c>
/// number from its PostgreSQL sequence (BR-PUR-014 — the same mechanism as <c>PUR-</c>, not a
/// second one), snapshots the supplier from the originating invoice, and persists the draft in a
/// single <c>SaveChanges</c> (STD-BE-024).
///
/// <para>Returns <c>null</c> when the invoice does not exist (404). An invoice that is not
/// <b>Received</b> is rejected with VTF-PUR-015 (BR-PUR-015 → 409): a draft never entered stock
/// and a cancelled one never will, so a return against either could only ever fail later at the
/// floor rule — rejecting here makes it early and legible instead of late and cryptic.</para>
/// </summary>
public sealed class CreatePurchaseReturnCommandHandler(VetFlowDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<CreatePurchaseReturnCommand, CreatePurchaseReturnResult?>
{
    public async Task<CreatePurchaseReturnResult?> HandleAsync(
        CreatePurchaseReturnCommand command,
        CancellationToken cancellationToken)
    {
        // The validator guarantees both are present before the handler runs.
        var invoiceId = command.PurchaseInvoiceId!.Value;

        var invoice = await dbContext.PurchaseInvoices
            .FirstOrDefaultAsync(item => item.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        if (invoice.Status != PurchaseInvoiceStatus.Received)
        {
            throw new BusinessRuleException(
                PurchasingErrorCodes.ReturnOriginalInvoiceNotReceived,
                new Dictionary<string, string> { ["status"] = invoice.Status.ToString() });
        }

        var sequenceValue = await dbContext.Database
            .SqlQueryRaw<long>($"SELECT nextval('{InternalPurchaseReturnNumber.SequenceName}') AS \"Value\"")
            .SingleAsync(cancellationToken);
        var number = InternalPurchaseReturnNumber.Format(sequenceValue);

        var purchaseReturn = new PurchaseReturn(
            Guid.NewGuid(),
            number,
            invoice.Id,
            invoice.SupplierName,
            command.ReturnDate!.Value,
            timeProvider.GetUtcNow(),
            command.Notes);

        dbContext.PurchaseReturns.Add(purchaseReturn);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreatePurchaseReturnResult { Id = purchaseReturn.Id, Number = purchaseReturn.Number };
    }
}
