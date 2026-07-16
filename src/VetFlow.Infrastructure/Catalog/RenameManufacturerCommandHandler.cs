using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Commands.RenameManufacturer;
using VetFlow.Application.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// Rename-manufacturer write path (REQ-CAT-013) — non-audited in the first version
/// (BR-CAT-053). Loads the aggregate, re-checks name uniqueness excluding itself
/// (BR-CAT-007), applies the rename through the domain, and persists in a single
/// <c>SaveChanges</c> (STD-BE-024). Returns the id, or <c>null</c> when the
/// manufacturer does not exist (→ 404). Products that reference it reflect the new
/// name through their join.
/// </summary>
public sealed class RenameManufacturerCommandHandler(VetFlowDbContext dbContext)
    : ICommandHandler<RenameManufacturerCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(RenameManufacturerCommand command, CancellationToken cancellationToken)
    {
        var manufacturer = await dbContext.Manufacturers
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

        if (manufacturer is null)
        {
            return null;
        }

        await ManufacturerNameUniqueness.EnsureUniqueAsync(dbContext, command.Name, command.Id, cancellationToken);

        manufacturer.Rename(command.Name);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (ManufacturerNameUniqueness.IsDuplicateNameViolation(exception))
        {
            throw ManufacturerNameUniqueness.DuplicateName();
        }

        return manufacturer.Id;
    }
}
