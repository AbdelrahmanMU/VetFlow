using Microsoft.EntityFrameworkCore;
using Npgsql;
using VetFlow.Application.Common;
using VetFlow.Domain.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Categories;

/// <summary>
/// Category name uniqueness after Arabic normalization (BR-CTG-003), shared by the
/// create and rename handlers. Two layers, both required:
/// <list type="bullet">
/// <item>a normalized pre-check that yields a friendly per-field 400 (TS-CTG-002);</item>
/// <item>the database unique index as the true guarantee under concurrency —
/// its violation (SQLSTATE 23505) is mapped back to the same 400, so the tiny race
/// window between the pre-check and the insert never leaks a raw 500.</item>
/// </list>
/// The normalized key is the shared <see cref="SearchableText"/> column, which for a
/// category equals <see cref="ArabicSearchText.Normalize(string, string?)"/> of the
/// name — safe to reuse as the uniqueness key precisely because a category has an
/// Arabic name only (BR-CTG-001); if an English name is ever added, this coupling
/// must be revisited (contrast the product's separate ArabicNameNormalized column).
/// </summary>
internal static class CategoryNameUniqueness
{
    /// <summary>The database unique index name — must match the EF configuration and migration.</summary>
    public const string UniqueIndexName = "ix_categories_name_unique";

    /// <summary>Throws a per-field <see cref="ValidationException"/> if another category already uses the normalized name.</summary>
    public static async Task EnsureUniqueAsync(
        VetFlowDbContext dbContext,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var normalized = ArabicSearchText.Normalize(name.Trim());

        var clash = await dbContext.Categories
            .AsNoTracking()
            .Where(category => EF.Property<string>(category, SearchableText.PropertyName) == normalized)
            .Where(category => excludeId == null || category.Id != excludeId)
            .AnyAsync(cancellationToken);

        if (clash)
        {
            throw DuplicateName();
        }
    }

    /// <summary>True when a save failed on the category-name unique index (the concurrent-insert race).</summary>
    public static bool IsDuplicateNameViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: UniqueIndexName,
        };

    public static ValidationException DuplicateName() =>
        new(new Dictionary<string, string[]>
        {
            ["Name"] = [ValidationMessageKeys.CategoryNameDuplicate],
        });
}
