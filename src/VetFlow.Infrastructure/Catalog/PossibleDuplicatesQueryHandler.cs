using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Catalog.Queries.PossibleDuplicates;
using VetFlow.Application.Common;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// The possible-duplicate advisory read (DEC-CAT-027): a trigram-similar
/// normalized Arabic name (pg_trgm <c>similarity() &gt;= threshold</c>, reusing
/// the Slice-1 write-time normalization) AND the same manufacturer. Advisory
/// only — the create write never consults it (BR-CAT-042 / DEC-CAT-018). Ordered
/// most-similar first and capped; the envelope is the fixed collection shape.
/// </summary>
public sealed class PossibleDuplicatesQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<PossibleDuplicatesQuery, PagedResult<PossibleDuplicateDto>>
{
    public async Task<PagedResult<PossibleDuplicateDto>> HandleAsync(
        PossibleDuplicatesQuery query,
        CancellationToken cancellationToken)
    {
        var normalizedInput = ArabicSearchText.Normalize(query.ArabicName.Trim());

        var matches =
            from product in dbContext.Products.AsNoTracking()
            where product.ManufacturerId == query.ManufacturerId
                && EF.Functions.TrigramsSimilarity(
                        EF.Property<string>(product, NormalizedArabicName.PropertyName), normalizedInput)
                    >= PossibleDuplicatesQuery.SimilarityThreshold
            join manufacturer in dbContext.Manufacturers.AsNoTracking() on product.ManufacturerId equals manufacturer.Id
            orderby EF.Functions.TrigramsSimilarity(
                EF.Property<string>(product, NormalizedArabicName.PropertyName), normalizedInput) descending
            select new PossibleDuplicateDto
            {
                Id = product.Id,
                ArabicName = product.ArabicName,
                EnglishName = product.EnglishName,
                Size = product.Size,
                Concentration = product.Concentration,
                ManufacturerName = manufacturer.Name,
            };

        var totalCount = await matches.CountAsync(cancellationToken);
        var items = await matches.Take(PossibleDuplicatesQuery.MaxResults).ToListAsync(cancellationToken);

        return new PagedResult<PossibleDuplicateDto>
        {
            Items = items,
            Page = 1,
            PageSize = PossibleDuplicatesQuery.MaxResults,
            TotalCount = totalCount,
        };
    }
}
