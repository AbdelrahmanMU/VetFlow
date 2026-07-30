using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Queries.BatchViewer;

/// <summary>
/// The batch viewer (REQ-INV-003): a read-only per-product view of every inventory
/// batch (active and depleted — BR-INV-019). Filter by batch status (BR-INV-021),
/// "expired" and "expiring soon" (BR-INV-026, reusing the 30-day horizon BR-INV-013),
/// whitelisted sorting (BR-INV-027), and offset pagination. The default order is the
/// receive date descending, tie-broken by the stable batch identifier ascending
/// (BR-INV-031). A read-only projection over the write-kernel state; it owns no
/// inventory state (BR-INV-018).
/// </summary>
public sealed record BatchViewerQuery : IQuery<BatchViewerResult?>
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Upper bound on the page number so the handler's Int32 offset
    /// <c>(Page - 1) * PageSize</c> can never overflow (STD-BE-027).
    /// </summary>
    public const int MaxPage = int.MaxValue / MaxPageSize;

    /// <summary>Fixed "expiring soon" horizon — 30 calendar days (BR-INV-013, DEC-INV-005).</summary>
    public const int ExpiringSoonHorizonDays = 30;

    /// <summary>The product whose batches are shown — the screen's scope (BR-INV-019).</summary>
    public required Guid ProductId { get; init; }

    /// <summary>When set, keep only batches of this status (BR-INV-026); null keeps both.</summary>
    public BatchStatus? Status { get; init; }

    /// <summary>When true, keep only batches whose expiry is in the past (BR-INV-026).</summary>
    public bool? Expired { get; init; }

    /// <summary>When true, keep only batches expiring within the 30-day horizon (BR-INV-026).</summary>
    public bool? ExpiringSoon { get; init; }

    public BatchViewerSortField Sort { get; init; } = BatchViewerSortField.ReceiveDate;

    public SortDirection Direction { get; init; } = SortDirection.Descending;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;
}
