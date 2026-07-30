using VetFlow.Application.Common;

namespace VetFlow.Application.Inventory.Queries.BatchViewer;

/// <summary>
/// The batch viewer response (REQ-INV-003): the product header (name + stock unit) plus
/// the paged batch rows. A null result from the handler means the product does not exist
/// (the "not found" state, AC-INV-022); a non-null result with an empty page means the
/// product exists but has no batches (the "empty" state). This lets one product-existence
/// guard distinguish the two states the acceptance criteria require, keeping the batch
/// list itself a single projection query with no per-row lookups (BR-INV-030).
/// </summary>
public sealed record BatchViewerResult
{
    public required string ProductName { get; init; }

    public required string StockUnitName { get; init; }

    public required PagedResult<BatchViewerItemDto> Batches { get; init; }
}
