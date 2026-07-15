using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Common;

/// <summary>
/// Shared implementation of the managed-lookup option queries: normalized-text
/// search (STD-BE-044), name ordering, and the fixed pagination envelope.
/// </summary>
public static class LookupOptionsQueryHandler
{
    private const string LikeEscapeCharacter = "\\";

    public static async Task<PagedResult<LookupOptionDto>> HandleAsync<TEntity>(
        IQueryable<TEntity> source,
        LookupOptionsQuery query,
        System.Linq.Expressions.Expression<Func<TEntity, LookupOptionDto>> projection,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(ArabicSearchText.Normalize(query.Search.Trim()))}%";
            source = source.Where(entity =>
                EF.Functions.ILike(EF.Property<string>(entity, SearchableText.PropertyName), pattern, LikeEscapeCharacter));
        }

        var options = source.Select(projection).OrderBy(option => option.Name);

        var totalCount = await options.CountAsync(cancellationToken);
        var items = await options
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LookupOptionDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        };
    }

    private static string EscapeLike(string value) =>
        value.Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);
}
