using Microsoft.EntityFrameworkCore;
using Npgsql;
using VetFlow.Application.Common;
using VetFlow.Domain.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// Manufacturer name uniqueness after Arabic normalization, shared by the create and
/// rename handlers. Two layers, both required:
/// <list type="bullet">
/// <item>a normalized pre-check that yields a friendly per-field 400;</item>
/// <item>the database unique index as the true guarantee under concurrency —
/// its violation (SQLSTATE 23505) is mapped back to the same 400, so the tiny race
/// window between the pre-check and the insert never leaks a raw 500.</item>
/// </list>
/// The normalized key is the shared <see cref="SearchableText"/> column, which for a
/// manufacturer equals <see cref="ArabicSearchText.Normalize(string, string?)"/> of
/// the name — safe to reuse as the uniqueness key precisely because a manufacturer
/// has an Arabic name only (BR-CAT-007, REQ-CAT-013); if an English name is ever
/// added, this coupling must be revisited. A deliberate copy of
/// <c>CategoryNameUniqueness</c> (Categories owns its own version).
/// </summary>
internal static class ManufacturerNameUniqueness
{
    /// <summary>The database unique index name — must match the EF configuration and migration.</summary>
    public const string UniqueIndexName = "ix_manufacturers_name_unique";

    /// <summary>Throws a per-field <see cref="ValidationException"/> if another manufacturer already uses the normalized name.</summary>
    public static async Task EnsureUniqueAsync(
        VetFlowDbContext dbContext,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var normalized = ArabicSearchText.Normalize(name.Trim());

        var clash = await dbContext.Manufacturers
            .AsNoTracking()
            .Where(manufacturer => EF.Property<string>(manufacturer, SearchableText.PropertyName) == normalized)
            .Where(manufacturer => excludeId == null || manufacturer.Id != excludeId)
            .AnyAsync(cancellationToken);

        if (clash)
        {
            throw DuplicateName();
        }
    }

    /// <summary>True when a save failed on the manufacturer-name unique index (the concurrent-insert race).</summary>
    public static bool IsDuplicateNameViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: UniqueIndexName,
        };

    public static ValidationException DuplicateName() =>
        new(new Dictionary<string, string[]>
        {
            ["Name"] = [ValidationMessageKeys.ManufacturerNameDuplicate],
        });
}
