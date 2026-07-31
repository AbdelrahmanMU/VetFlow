using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Commands.RemovePurchaseReturnLine;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Remove-purchase-return-line write path (BR-PUR-018). Returns <c>false</c> when the return or the
/// line does not exist (404); a committed return is rejected by the aggregate with VTF-PUR-018
/// (→ 409) without mutation. Mirrors the remove-purchase-line-item handler.
/// </summary>
public sealed class RemovePurchaseReturnLineCommandHandler(VetFlowDbContext dbContext)
    : ICommandHandler<RemovePurchaseReturnLineCommand, bool>
{
    public async Task<bool> HandleAsync(RemovePurchaseReturnLineCommand command, CancellationToken cancellationToken)
    {
        var purchaseReturn = await dbContext.PurchaseReturns
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == command.PurchaseReturnId, cancellationToken);

        if (purchaseReturn is null)
        {
            return false;
        }

        // The Draft-only guard lives on the aggregate (BR-PUR-018) and throws before any removal.
        if (!purchaseReturn.RemoveLine(command.PurchaseReturnLineId))
        {
            return false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
