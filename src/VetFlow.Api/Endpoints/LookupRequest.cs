using VetFlow.Application.Common;

namespace VetFlow.Api.Endpoints;

/// <summary>Raw query-string surface shared by the managed-lookup option endpoints.</summary>
public sealed record LookupRequest(string? Search, string? Page, string? PageSize)
{
    public TQuery ToQuery<TQuery>(Func<string?, int, int, TQuery> factory)
        where TQuery : LookupOptionsQuery
    {
        var parser = new QueryStringParser();

        var search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        var page = parser.ParseInteger(Page, "page", 1);
        var pageSize = Math.Min(
            parser.ParseInteger(PageSize, "pageSize", LookupOptionsQuery.DefaultPageSize),
            LookupOptionsQuery.MaxPageSize);

        parser.ThrowIfInvalid();
        return factory(search, page, pageSize);
    }
}
