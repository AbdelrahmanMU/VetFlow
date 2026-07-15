using VetFlow.Application.Catalog.Queries.PossibleDuplicates;
using VetFlow.Application.Common;

namespace VetFlow.Api.Endpoints.Catalog;

/// <summary>
/// Query-string surface of GET /api/v1/products/possible-duplicates — explicit
/// whitelisted parameters (STD-API-023); a malformed manufacturer id becomes the
/// canonical validation shape rather than a bare binding failure.
/// </summary>
public sealed record PossibleDuplicatesRequest(string? ArabicName, string? ManufacturerId)
{
    public PossibleDuplicatesQuery ToQuery()
    {
        var parser = new QueryStringParser();
        var query = new PossibleDuplicatesQuery
        {
            ArabicName = ArabicName?.Trim() ?? string.Empty,
            ManufacturerId = parser.ParseId(ManufacturerId, "manufacturerId") ?? Guid.Empty,
        };

        parser.ThrowIfInvalid();
        return query;
    }
}
