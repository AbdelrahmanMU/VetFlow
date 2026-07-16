using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Categories.Commands.SetCategoryActive;
using VetFlow.Application.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Categories;

/// <summary>
/// Activate/deactivate-category write path (REQ-CTG-004, BR-CTG-004). Loads the
/// aggregate, applies the state change through the domain, and persists in a single
/// <c>SaveChanges</c> (STD-BE-024). Deactivation is always allowed and never touches
/// referencing products (DEC-CTG-002, option B). Returns the id, or <c>null</c> when
/// the category does not exist (→ 404).
/// </summary>
public sealed class SetCategoryActiveCommandHandler(VetFlowDbContext dbContext)
    : ICommandHandler<SetCategoryActiveCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(SetCategoryActiveCommand command, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

        if (category is null)
        {
            return null;
        }

        if (command.IsActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return category.Id;
    }
}
