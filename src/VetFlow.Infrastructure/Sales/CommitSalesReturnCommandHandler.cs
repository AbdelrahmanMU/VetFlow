using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Inventory;
using VetFlow.Application.Sales.Commands.CommitSalesReturn;
using VetFlow.Domain.Common;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Commit-sales-return write path (BR-SAL-018, AC-SAL-019/020) — the one place C6 moves stock.
///
/// <para>The state transition belongs to the aggregate; the stock effect belongs to Inventory, and
/// this handler joins them in <b>one</b> unit of work. It states the intent — "put sale line L's
/// portion back" — through the Inventory contract (<see cref="IInventorySalesReturnWriter"/>,
/// BR-SAL-013, DEC-SAL-006) and <b>never touches a batch itself</b>: which batches receive the
/// quantity, in what order, and in what unit is read by Inventory from the recorded consumption
/// trace (BR-SAL-017, REQ-INV-008). This is the one structural difference from the purchase-return
/// handler, and it is required by Sales' own isolation rule rather than chosen.</para>
///
/// <para><b>The ordering is load-bearing.</b> The aggregate is transitioned <i>before</i> Inventory
/// applies, so the single <c>SaveChanges</c> inside the shared batch writer commits the status, the
/// batch increases, the on-hand increases and the ledger rows together or not at all (BR-INV-062) —
/// the C5 arrangement unchanged, and what AC-SAL-019 asserts.</para>
///
/// <para>What each line tells Inventory is three facts of the <b>Sales</b> documents: the original
/// line's sold quantity, what earlier <b>committed</b> returns already took from it, and what this
/// document returns now (BR-SAL-016). The middle one is what makes a second partial return resume
/// where the first stopped instead of refilling a batch already made whole.</para>
///
/// <para>Returns <c>false</c> when the return does not exist (404). A committed return
/// (VTF-SAL-018), an empty one (VTF-SAL-019), an unusable consumption trace (VTF-SAL-020) or a
/// concurrent batch change (VTF-INV-068) are all rejected with nothing applied.</para>
/// </summary>
public sealed class CommitSalesReturnCommandHandler(
    VetFlowDbContext dbContext,
    IInventorySalesReturnWriter inventoryWriter)
    : ICommandHandler<CommitSalesReturnCommand, bool>
{
    public async Task<bool> HandleAsync(CommitSalesReturnCommand command, CancellationToken cancellationToken)
    {
        var salesReturn = await dbContext.SalesReturns
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == command.SalesReturnId, cancellationToken);

        if (salesReturn is null)
        {
            return false;
        }

        // Transition first: it enforces Draft-only and non-empty (BR-SAL-018) and throws before any
        // stock is touched, so a rejected commit cannot leave a partial movement behind.
        salesReturn.Commit();

        var lineIds = salesReturn.Lines.Select(line => line.SalesLineItemId).Distinct().ToList();
        var soldQuantities = await dbContext.SalesLineItems
            .Where(line => lineIds.Contains(line.Id))
            .ToDictionaryAsync(line => line.Id, line => line.Quantity, cancellationToken);

        // Where each sale line's return resumes. It starts at what earlier committed returns took
        // and then advances **within this document too**: two lines of one return may name the same
        // sale line (the add-line ceiling allows it, counting both), and without the running offset
        // they would both map onto the same slice of the trace and put the quantity back twice.
        var resumeAt = new Dictionary<Guid, decimal>();

        var requests = new List<InventorySalesReturnRequest>(salesReturn.Lines.Count);
        foreach (var line in salesReturn.Lines.OrderBy(line => line.AddedAt).ThenBy(line => line.Id))
        {
            if (!soldQuantities.TryGetValue(line.SalesLineItemId, out var soldQuantity))
            {
                // The originating sale line vanished between add and commit. Committing the document
                // without its stock effect would break BR-INV-062, so it fails loudly.
                throw new BusinessRuleException(
                    SalesErrorCodes.ReturnLineComposition,
                    new Dictionary<string, string> { ["reason"] = "originalLineMissing" });
            }

            if (!resumeAt.TryGetValue(line.SalesLineItemId, out var previouslyReturned))
            {
                // Excludes this document deliberately: its own quantities are what is being applied
                // now, and counting them would skip their own share of the trace (BR-SAL-016/017).
                previouslyReturned = await SalesReturnableQuantities.GetAlreadyReturnedForLineAsync(
                    dbContext, line.SalesLineItemId, salesReturn.Id, cancellationToken);
            }

            requests.Add(new InventorySalesReturnRequest
            {
                SaleLineId = line.SalesLineItemId,
                ReturnLineId = line.Id,
                SoldQuantity = soldQuantity,
                PreviouslyReturnedQuantity = previouslyReturned,
                ReturnQuantity = line.Quantity,
            });

            resumeAt[line.SalesLineItemId] = previouslyReturned + line.Quantity;
        }

        await inventoryWriter.ApplyAsync(requests, cancellationToken);
        return true;
    }
}
