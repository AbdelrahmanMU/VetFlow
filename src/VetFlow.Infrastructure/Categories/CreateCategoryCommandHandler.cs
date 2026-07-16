using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Categories.Commands.CreateCategory;
using VetFlow.Application.Common;
using VetFlow.Domain.Categories;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Categories;

/// <summary>
/// Create-category write path (REQ-CTG-002). Enforces name uniqueness after Arabic
/// normalization (BR-CTG-003), builds the aggregate through the domain constructor
/// (active by default — BR-CTG-004), and persists it in a single
/// <c>SaveChanges</c> (STD-BE-024). The normalized search column is maintained by
/// <see cref="SearchTextInterceptor"/>.
/// </summary>
public sealed class CreateCategoryCommandHandler(VetFlowDbContext dbContext)
    : ICommandHandler<CreateCategoryCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        await CategoryNameUniqueness.EnsureUniqueAsync(dbContext, command.Name, excludeId: null, cancellationToken);

        var category = new Category(Guid.NewGuid(), command.Name);
        dbContext.Categories.Add(category);

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
