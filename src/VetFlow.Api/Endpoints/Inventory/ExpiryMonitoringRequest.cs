using VetFlow.Application.Inventory.Queries.ExpiryMonitoring;

namespace VetFlow.Api.Endpoints.Inventory;

/// <summary>
/// Raw query-string surface of GET /api/v1/inventory/expiry — the whitelisted search,
/// category, "expired" and "expiring soon" filters, and pagination parameters (BR-INV-035,
/// STD-API-023), parsed explicitly so every malformed value produces the canonical validation
/// shape. There is no user-selectable sort — the order is fixed (BR-INV-037).
/// </summary>
public sealed record ExpiryMonitoringRequest(
    string? Search,
    string? Category,
    string? Expired,
    string? ExpiringSoon,
    string? Page,
    string? PageSize)
{
    public ExpiryMonitoringQuery ToQuery()
    {
        var parser = new QueryStringParser();

        var query = new ExpiryMonitoringQuery
        {
            Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim(),
            CategoryId = parser.ParseId(Category, "category"),
            Expired = parser.ParseBoolean(Expired, "expired"),
            ExpiringSoon = parser.ParseBoolean(ExpiringSoon, "expiringSoon"),
            Page = parser.ParseInteger(Page, "page", 1),
            PageSize = Math.Min(
                parser.ParseInteger(PageSize, "pageSize", ExpiryMonitoringQuery.DefaultPageSize),
                ExpiryMonitoringQuery.MaxPageSize),
        };

        parser.ThrowIfInvalid();
        return query;
    }
}
