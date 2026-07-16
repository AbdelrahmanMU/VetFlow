using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Commands.CreateManufacturer;
using VetFlow.Application.Common;
using VetFlow.Domain.Catalog;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// Create-manufacturer write path (REQ-CAT-013). Enforces name uniqueness after
/// Arabic normalization (BR-CAT-007), builds the aggregate through the domain
/// constructor (active by default — BR-CAT-052), and persists it in a single
/// <c>SaveChanges</c> (STD-BE-024). The normalized search column is maintained by
/// <see cref="SearchTextInterceptor"/>.
/// </summary>
public sealed class CreateManufacturerCommandHandler(VetFlowDbContext dbContext)
    : ICommandHandler<CreateManufacturerCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateManufacturerCommand command, CancellationToken cancellationToken)
    {
        await ManufacturerNameUniqueness.EnsureUniqueAsync(dbContext, command.Name, excludeId: null, cancellationToken);

        var manufacturer = new Manufacturer(Guid.NewGuid(), command.Name);
        dbContext.Manufacturers.Add(manufacturer);

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
