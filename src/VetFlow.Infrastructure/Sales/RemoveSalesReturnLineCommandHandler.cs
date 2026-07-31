using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Sales.Commands.RemoveSalesReturnLine;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Remove-sales-return-line write path (BR-SAL-018). Returns <c>false</c> when the return or the
/// line does not exist (404); a committed return is rejected by the aggregate with VTF-SAL-018
/// (→ 409) without mutation. Mirrors the remove-sales-line-item handler.
/// </summary>
public sealed class RemoveSalesReturnLineCommandHandler(VetFlowDbContext dbContext)
    : ICommandHandler<RemoveSalesReturnLineCommand, bool>
{
    public async Task<bool> HandleAsync(RemoveSalesReturnLineCommand command, CancellationToken cancellationToken)
    {
        var salesReturn = await dbContext.SalesReturns
            .Include(item => item.Lines)
            .FirstOrDefaultAsync(item => item.Id == command.SalesReturnId, cancellationToken);

        if (salesReturn is null)
        {
            return false;
        }

        // The Draft-only guard lives on the aggregate (BR-SAL-018) and throws before any removal.
        if (!salesReturn.RemoveLine(command.SalesReturnLineId))
        {
            return false;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
