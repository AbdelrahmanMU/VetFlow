using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Categories.Commands.RenameCategory;
using VetFlow.Application.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Categories;

/// <summary>
/// Rename-category write path (REQ-CTG-003) — non-audited in the first version
/// (BR-CTG-007). Loads the aggregate, re-checks name uniqueness excluding itself
/// (BR-CTG-003), applies the rename through the domain, and persists in a single
/// <c>SaveChanges</c> (STD-BE-024). Returns the id, or <c>null</c> when the
/// category does not exist (→ 404). Products that reference it reflect the new name
/// through their join (AC-CTG-003).
/// </summary>
public sealed class RenameCategoryCommandHandler(VetFlowDbContext dbContext)
    : ICommandHandler<RenameCategoryCommand, Guid?>
{
    public async Task<Guid?> HandleAsync(RenameCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories
            .FirstOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);

        if (category is null)
        {
            return null;
        }

        await CategoryNameUniqueness.EnsureUniqueAsync(dbContext, command.Name, command.Id, cancellationToken);

        category.Rename(command.Name);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (CategoryNameUniqueness.IsDuplicateNameViolation(exception))
        {
            throw CategoryNameUniqueness.DuplicateName();
        }

        return category.Id;
    }
}
