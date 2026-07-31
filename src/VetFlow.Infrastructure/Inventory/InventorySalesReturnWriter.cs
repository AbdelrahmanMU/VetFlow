using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Inventory;
using VetFlow.Domain.Common;
using VetFlow.Domain.Inventory;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Inventory;

/// <summary>
/// The Inventory sales-return path (BR-INV-069, REQ-SAL-004) — the implementation of the public
/// contract a committing sales return depends on (<see cref="IInventorySalesReturnWriter"/>). It is
/// the inbound mirror of <see cref="InventoryConsumptionWriter"/>: there, Sales stated "consume N of
/// product P for sale line L" and Inventory chose the batches by FEFO; here, Sales states "put sale
/// line L's portion back" and Inventory <b>reads</b> the batches from what that FEFO run actually
/// recorded.
///
/// <para><b>Nothing is selected and FEFO is never consulted</b> (BR-INV-069, BR-SAL-017): the
/// destination is the <c>Consume</c> movements of that sale line (REQ-INV-008, BR-INV-057), which
/// are the only record of where the goods went. <b>Expired batches are not excluded either</b> —
/// this is the one place that predicate must <i>not</i> be copied from the consumption writer: the
/// quantity goes back exactly where it came from, and refusing an expired batch would strand stock
/// with no home while the ban on <i>selling</i> it stays untouched (DEC-INV-021, BR-SAL-018,
/// TS-SAL-022).</para>
///
/// <para><b>Two derivations, both from recorded data rather than from today's configuration</b> —
/// the same reasoning C5 used for its receipt-derived conversion factor (BR-PUR-016):</para>
/// <list type="number">
///   <item><description><b>The unit conversion.</b> A return line's quantity is in the original
///   sale line's sale unit; batches hold the smallest stock unit (BR-INV-058). The factor used is
///   the one that <i>actually applied</i> — total consumed stock over the line's sold quantity — not
///   the catalog's factor today, which may have been edited since and would move the wrong
///   amount.</description></item>
///   <item><description><b>The consumption order.</b> FEFO consumed the line's batches in the total
///   order of BR-INV-050 (expiry ascending, nulls last, then receive date, then batch id), and every
///   component of that key is immutable after receiving — so re-deriving it reproduces the order the
///   goods actually left in. The ledger's own <c>(OccurredAt, Id)</c> sort deliberately is
///   <b>not</b> used: a sale's movements all share one timestamp and the tie-break id is random, so
///   it would give a stable order that is not the consumption order.</description></item>
/// </list>
///
/// <para><b>How a partial return is distributed</b> (BR-SAL-017, AC-SAL-018). The line's sold
/// quantity is mapped onto its consumption sequence proportionally — position <c>x</c> of the line
/// sits at <c>x·T/Q</c> of the consumed stock — and this document returns the slice between what
/// earlier committed returns already took and where it now ends. A first return of 8 against
/// (6 from A, 4 from B) fills A then B: A +6, B +2. A later return of 2 resumes at 8 and goes to
/// B alone — it does not refill A, which has already been made whole. Multiplication happens before
/// division so no factor is ever materialized on its own, and a full return lands exactly on
/// <c>T</c> with no residue (BR-INV-058: quantities are never rounded).</para>
/// </summary>
public sealed class InventorySalesReturnWriter(VetFlowDbContext dbContext, BatchOperationWriter batchWriter)
    : IInventorySalesReturnWriter
{
    /// <summary>
    /// The decimal places the batch and on-hand columns keep (<c>numeric(18,3)</c>). A movement that
    /// cannot be expressed exactly at this scale is rejected rather than rounded — rounding would
    /// break the BR-INV-005 invariant silently, with no adjustment path to correct it (BR-INV-058).
    /// </summary>
    private const int StockQuantityDecimals = 3;

    public async Task ApplyAsync(
        IReadOnlyCollection<InventorySalesReturnRequest> requests,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfZero(requests.Count);

        var traces = await ReadConsumptionTracesAsync(requests, cancellationToken);

        var deltas = new List<BatchOperationWriter.DocumentBatchDelta>(requests.Count);
        foreach (var request in requests)
        {
            deltas.AddRange(Distribute(request, traces));
        }

        // The caller has already staged its Draft → Committed transition, so this single
        // SaveChanges — inside ApplyDocumentAsync — commits the document status, every batch, every
        // on-hand quantity and every ledger row together or not at all (BR-SAL-018, BR-INV-062).
        // That ordering is load-bearing and is what AC-SAL-019 asserts.
        var movementIds = await batchWriter.ApplyDocumentAsync(
            deltas,
            InventoryMovementType.SalesReturn,
            InventoryMovementSource.Sales,
            cancellationToken);

        if (movementIds is null)
        {
            // A batch named by the trace no longer exists. That is not a business outcome the user
            // can act on, and committing the document without its stock effect would break
            // BR-INV-062 — so it fails loudly with nothing saved.
            throw new BusinessRuleException(
                SalesErrorCodes.ReturnConsumptionTraceUnusable,
                new Dictionary<string, string> { ["reason"] = "batchMissing" });
        }
    }

    /// <summary>
    /// The <c>Consume</c> movements of every requested sale line, in <b>consumption order</b> — one
    /// query for all of them regardless of how many lines or batches are involved (BR-INV-053).
    /// Ordering is applied in memory because it is per sale line and its key spans the batch.
    /// </summary>
    private async Task<Dictionary<Guid, List<ConsumedFromBatch>>> ReadConsumptionTracesAsync(
        IReadOnlyCollection<InventorySalesReturnRequest> requests,
        CancellationToken cancellationToken)
    {
        var saleLineIds = requests.Select(request => request.SaleLineId).Distinct().ToList();

        var rows = await (
            from movement in dbContext.InventoryMovements.AsNoTracking()
            join batch in dbContext.InventoryBatches.AsNoTracking() on movement.BatchId equals batch.Id
            where movement.Type == InventoryMovementType.Consume
                && movement.ReferenceId != null
                && saleLineIds.Contains(movement.ReferenceId.Value)
            select new
            {
                SaleLineId = movement.ReferenceId!.Value,
                movement.BatchId,
                movement.Quantity,
                batch.ExpiryDate,
                batch.ReceivedAt,
            }).ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.SaleLineId)
            .ToDictionary(
                group => group.Key,
                group => group
                    // The BR-INV-050 total order FEFO allocated in: expiry ascending with nulls
                    // last — stated explicitly, never left to the database — then receive date, then
                    // the stable batch id.
                    .OrderBy(row => row.ExpiryDate == null)
                    .ThenBy(row => row.ExpiryDate)
                    .ThenBy(row => row.ReceivedAt)
                    .ThenBy(row => row.BatchId)
                    // Consumption is stored negative (BR-INV-064); the magnitude is what left.
                    .Select(row => new ConsumedFromBatch(row.BatchId, -row.Quantity))
                    .ToList());
    }

    /// <summary>
    /// Map one return line onto its sale line's consumption sequence (BR-SAL-017) and produce one
    /// positive delta per batch that owes something back.
    /// </summary>
    private static List<BatchOperationWriter.DocumentBatchDelta> Distribute(
        InventorySalesReturnRequest request,
        IReadOnlyDictionary<Guid, List<ConsumedFromBatch>> traces)
    {
        if (!traces.TryGetValue(request.SaleLineId, out var trace) || trace.Count == 0)
        {
            // No trace at all. Either the sale predates the movement ledger — the C1 migration
            // replaced the Sprint 7 consumption table without backfilling it — or the document and
            // the ledger have diverged. Both mean the destination cannot be known, and BR-SAL-017
            // leaves no other way to find one: a return may never invent a batch the goods did not
            // leave from. Rejected with nothing saved.
            throw new BusinessRuleException(
                SalesErrorCodes.ReturnConsumptionTraceUnusable,
                new Dictionary<string, string> { ["reason"] = "traceMissing" });
        }

        var consumedTotal = trace.Sum(entry => entry.Quantity);
        if (consumedTotal <= 0m || request.SoldQuantity <= 0m)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.ReturnConsumptionTraceUnusable,
                new Dictionary<string, string> { ["reason"] = "traceEmpty" });
        }

        // The returnable ceiling, re-checked here at commit — and this is the case BR-SAL-016
        // explicitly predicts rather than an edge case: drafts do not reserve, so two drafts can
        // each pass the add-line check against the same remainder and **the second fails here**.
        // It must fail as an over-return, which is what actually happened and what the screen has a
        // message for; without this guard the walk below would come up short and report the trace
        // as unusable, misdiagnosing "someone returned it first" as "the ledger is unreadable".
        // Checked in the line's own sale unit — the unit BR-SAL-016 is written in.
        if (request.PreviouslyReturnedQuantity + request.ReturnQuantity > request.SoldQuantity)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.ReturnQuantityExceedsReturnable,
                new Dictionary<string, string>
                {
                    ["requested"] = request.ReturnQuantity.ToString("G", CultureInfo.InvariantCulture),
                    ["remaining"] = (request.SoldQuantity - request.PreviouslyReturnedQuantity)
                        .ToString("G", CultureInfo.InvariantCulture),
                });
        }

        // Multiply before dividing so the factor (T/Q) is never materialized on its own: a factor
        // like 1000/3 would lose fidelity, and quantities are never rounded (BR-INV-058). A full
        // return makes the numerator Q·T exactly, so the end position lands exactly on T.
        var start = request.PreviouslyReturnedQuantity * consumedTotal / request.SoldQuantity;
        var end = (request.PreviouslyReturnedQuantity + request.ReturnQuantity) * consumedTotal / request.SoldQuantity;

        // The quantity does not convert into whole stock units at the precision inventory keeps.
        // BR-INV-058 forbids rounding it, and letting the store round on write would break the
        // BR-INV-005 invariant silently — so the return is rejected, exactly as an inexact
        // conversion is rejected on the way out (BR-SAL-012).
        if (!IsRepresentable(start) || !IsRepresentable(end))
        {
            throw new BusinessRuleException(
                SalesErrorCodes.InexactUnitConversion,
                new Dictionary<string, string> { ["reason"] = "conversionNotExact" });
        }

        var deltas = new List<BatchOperationWriter.DocumentBatchDelta>();
        var distributed = 0m;
        var cursor = 0m;

        foreach (var entry in trace)
        {
            var segmentStart = cursor;
            var segmentEnd = cursor + entry.Quantity;
            cursor = segmentEnd;

            // How much of this batch's share falls inside the slice this document returns.
            var share = Math.Min(end, segmentEnd) - Math.Max(start, segmentStart);
            if (share <= 0m)
            {
                continue;
            }

            deltas.Add(new BatchOperationWriter.DocumentBatchDelta(entry.BatchId, share, request.ReturnLineId));
            distributed += share;
        }

        // Defensive: the walk must place the whole slice. A mismatch would mean the trace and the
        // arithmetic disagree, and moving a partial quantity anyway would break BR-INV-005.
        if (distributed != end - start || deltas.Count == 0)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.ReturnConsumptionTraceUnusable,
                new Dictionary<string, string> { ["reason"] = "traceShort" });
        }

        return deltas;
    }

    private static bool IsRepresentable(decimal value) =>
        decimal.Round(value, StockQuantityDecimals) == value;

    /// <summary>One recorded consumption: how much of a sale line left through a given batch.</summary>
    private readonly record struct ConsumedFromBatch(Guid BatchId, decimal Quantity);
}
