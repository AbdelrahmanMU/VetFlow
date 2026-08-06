using Microsoft.Extensions.Logging;
using VetFlow.Application.Common;
using VetFlow.Application.Dashboard.Queries.OperationalDashboard;
using VetFlow.Application.Inventory.Queries.InventoryDashboardSummary;
using VetFlow.Application.Purchasing.Queries.PurchasingDashboardSummary;
using VetFlow.Application.Sales.Queries.SalesDashboardSummary;

namespace VetFlow.Infrastructure.Dashboard;

/// <summary>
/// The operational dashboard (REQ-DSH-010) — <b>composition and nothing else</b>.
/// <para>
/// <b>This handler contains no business rule, no predicate and no arithmetic</b>, which is the
/// whole of BR-DSH-001 / DEC-DSH-001 as the owner reaffirmed it on 2026-08-03: «the Dashboard
/// is a read-composition module only and must never duplicate business logic or compute
/// domain rules». Every number below is produced by the module that owns its rule —
/// Inventory (REQ-INV-013), Sales (REQ-SAL-006), Purchasing (REQ-PUR-007) — and this class
/// only decides where each one is displayed. If you ever find yourself adding a
/// <c>Where</c>, a <c>Count</c> or a comparison here, the rule has been broken.
/// </para>
/// <para>
/// <b>Why the composition lives in Infrastructure.</b> Query handlers are the sanctioned
/// cross-module read path (ADR-0014 §2), and the module isolation test (STD-BE-005) forbids
/// one module's Application namespace from referencing another's. Keeping
/// <c>VetFlow.Application.Dashboard</c> to a parameterless query and a primitive DTO is what
/// lets the Dashboard be a real module rather than an exception to the boundary rules.
/// </para>
/// <para>
/// <b>Three reads, not seven.</b> One per owning module, each returning that module's own
/// facts. The consequence is recorded rather than hidden: the four inventory sections share a
/// source, so if Inventory cannot be read <b>all four fail together</b>. The contract still
/// holds — every section reports its own outcome, and none is ever shown as zero.
/// </para>
/// </summary>
public sealed class OperationalDashboardQueryHandler(
    IQueryHandler<InventoryDashboardSummaryQuery, InventoryDashboardSummaryDto> inventory,
    IQueryHandler<SalesDashboardSummaryQuery, SalesDashboardSummaryDto> sales,
    IQueryHandler<PurchasingDashboardSummaryQuery, PurchasingDashboardSummaryDto> purchasing,
    IClinicClock clinicClock,
    ILogger<OperationalDashboardQueryHandler> logger)
    : IQueryHandler<OperationalDashboardQuery, OperationalDashboardDto>
{
    public async Task<OperationalDashboardDto> HandleAsync(
        OperationalDashboardQuery query,
        CancellationToken cancellationToken)
    {
        var inventorySummary = await ReadSectionAsync(
            () => inventory.HandleAsync(new InventoryDashboardSummaryQuery(), cancellationToken),
            nameof(inventory),
            cancellationToken);

        var salesSummary = await ReadSectionAsync(
            () => sales.HandleAsync(new SalesDashboardSummaryQuery(), cancellationToken),
            nameof(sales),
            cancellationToken);

        var purchasingSummary = await ReadSectionAsync(
            () => purchasing.HandleAsync(new PurchasingDashboardSummaryQuery(), cancellationToken),
            nameof(purchasing),
            cancellationToken);

        return new OperationalDashboardDto
        {
            // Returned so the browser never derives a date of its own — device time is a
            // prohibited source (clinic-date.md). Read outside the try blocks: if the clock
            // itself cannot answer, the request has no business date and failing loudly is
            // correct — a silent fallback to UTC is exactly what BR-INV-060 forbids.
            ClinicDate = clinicClock.Today,
            Sections = new DashboardSectionsDto
            {
                ExpiredBatches = Count(inventorySummary, summary => summary.ExpiredBatchCount),
                OutOfStockProducts = Count(inventorySummary, summary => summary.OutOfStockProductCount),
                ExpiringSoonBatches = Count(inventorySummary, summary => summary.ExpiringSoonBatchCount),
                DraftPurchases = Count(purchasingSummary, summary => summary.DraftInvoiceCount),
                DraftSales = Count(salesSummary, summary => summary.DraftInvoiceCount),
                TodaySales = salesSummary is null
                    ? DashboardTodaySalesSectionDto.Failed()
                    : DashboardTodaySalesSectionDto.Ok(
                        salesSummary.TodayInvoiceCount,
                        salesSummary.TodayTotal),
                RecentMovements = inventorySummary is null
                    ? DashboardRecentMovementsSectionDto.Failed()
                    : DashboardRecentMovementsSectionDto.Ok(
                        [.. inventorySummary.RecentMovements.Select(ToDashboardMovement)]),
            },
        };
    }

    /// <summary>
    /// Projects one count, or marks the section failed. <b>Never substitutes zero</b>: "no
    /// expired batches" and "could not determine expired batches" are contradictory
    /// statements, and conflating them is false reassurance inside a safety decision
    /// (BR-DSH-014, DEC-INV-021).
    /// </summary>
    private static DashboardCountSectionDto Count<TSummary>(TSummary? summary, Func<TSummary, int> select)
        where TSummary : class =>
        summary is null ? DashboardCountSectionDto.Failed() : DashboardCountSectionDto.Ok(select(summary));

    /// <summary>
    /// Passes the owning module's movement through untouched — the type is stringified from
    /// Inventory's own closed vocabulary (BR-INV-065) rather than re-declared here, so the
    /// Dashboard cannot drift from it.
    /// </summary>
    private static DashboardMovementDto ToDashboardMovement(InventoryDashboardMovementDto movement) =>
        new()
        {
            MovementId = movement.MovementId,
            OccurredAt = movement.OccurredAt,
            Type = ToCamelCase(movement.Type.ToString()),
            ProductName = movement.ProductName,
            Quantity = movement.Quantity,
            StockUnitName = movement.StockUnitName,
        };

    private static string ToCamelCase(string value) =>
        string.Concat(char.ToLowerInvariant(value[0]), value[1..]);

    /// <summary>
    /// Runs one owning module's read, returning <c>null</c> if it fails so its sections can be
    /// marked failed while the rest of the board still renders (DEC-DSH-002).
    /// <para>
    /// <b>The broad catch is deliberate and is the point of the method.</b> A dashboard exists
    /// to be glanced at during a busy morning; one slow or broken module must not blank the
    /// other six sections. Cancellation is re-thrown rather than swallowed — an abandoned
    /// request is not a failed section, and reporting it as one would log noise for every
    /// navigation away from the page. The failure is logged at warning with the module that
    /// produced it, so a section that quietly fails in production is still visible to us.
    /// </para>
    /// </summary>
    private async Task<TSummary?> ReadSectionAsync<TSummary>(
        Func<Task<TSummary>> read,
        string module,
        CancellationToken cancellationToken)
        where TSummary : class
    {
        try
        {
            return await read();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
#pragma warning disable CA1031 // Deliberate: one module's failure must not blank the whole board.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            logger.LogWarning(
                exception,
                "Dashboard section from {Module} could not be read; it is reported as failed.",
                module);
            return null;
        }
    }
}
