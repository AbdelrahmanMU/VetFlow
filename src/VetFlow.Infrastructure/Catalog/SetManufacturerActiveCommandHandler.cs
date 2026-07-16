using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Commands.SetManufacturerActive;
using VetFlow.Application.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// Activate/deactivate-manufacturer write path (REQ-CAT-048, BR-CAT-052). Loads the
/// aggregate, applies the state change through the domain, and persists in a single
/// <c>SaveChanges</c> (STD-BE-024). Deactivation is always allowed and never touches
/// referencing products (DEC-CAT-032, option B). Returns the id, or <c>null</c> when
/// the manufacturer does not exist (→ 404).
/// </summary>
public sealed class SetManufacturerActiveCommandHandler(VetFlowDbContext dbContext)
    : ICommandHandler<SetManufacturerActiveCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(SetManufacturerActiveCommand command, CancellationToken cancellationToken)
    {
        var manufacturer = await dbContext.Manufacturers
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

        if (manufacturer is null)
        {
            return null;
        }

        if (command.IsActive)
        {
            manufacturer.Activate();
        }
        else
        {
            manufacturer.Deactivate();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return manufacturer.Id;
    }
}
