using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Queries.ExpiryMonitoring;

/// <summary>
/// Expiry monitoring (REQ-INV-004): a read-only, clinic-wide list of active batches with a
/// real expiry that are expired or expiring soon. Search by product name, filter by category,
/// "expired" and "expiring soon" (BR-INV-035/036, reusing the 30-day horizon BR-INV-013).
/// The order is deterministic — the expiry date ascending, tie-broken by the stable batch
/// identifier ascending (BR-INV-037); there is no user-selectable sort. A projection that owns
/// no expiry state — it is computed from <see cref="Domain.Inventory.InventoryBatch.ExpiryDate"/>
/// at query time (DEC-INV-018).
/// </summary>
public sealed record ExpiryMonitoringQuery : IQuery<PagedResult<ExpiryMonitoringItemDto>>
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
    public const int MaxSearchLength = 200;

    /// <summary>
    /// Upper bound on the page number so the handler's Int32 offset
    /// <c>(Page - 1) * PageSize</c> can never overflow (STD-BE-027).
    /// </summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;

    /// <summary>Fixed "expiring soon" horizon — 30 calendar days (BR-INV-013, DEC-INV-005).</summary>
    public const int ExpiringSoonHorizonDays = 30;

    public string? Search { get; init; }

    public Guid? CategoryId { get; init; }

    /// <summary>When true, keep only batches whose expiry is in the past (BR-INV-036).</summary>
    public bool? Expired { get; init; }

    /// <summary>When true, keep only batches expiring within the 30-day horizon (BR-INV-036).</summary>
    public bool? ExpiringSoon { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}
