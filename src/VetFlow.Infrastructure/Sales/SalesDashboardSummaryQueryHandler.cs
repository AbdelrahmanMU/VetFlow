using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Sales.Queries.SalesDashboardSummary;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Sales' dashboard read (REQ-SAL-006, BR-SAL-020). Read-only; it owns no state and changes
/// no invoice. Committed state only (BR-INV-016's convention, shared by every projection).
/// <para>
/// <b>"Today" is the clinic local date</b> from the single cross-cutting source
/// (<c>clinic-date.md</c>, owner ruling OQ-DSH-2) — captured in C# and compared inside the
/// query. UTC, server time and the caller's device are prohibited sources and none is read
/// here.
/// </para>
/// <para>
/// <b>The date compared is the header's <see cref="SalesInvoice.SaleDate"/>, not the commit
/// time</b> — the field BR-SAL-019 filters by — so this count and the sales list agree
/// (BR-DSH-018, AC-SAL-025). Pinned by test: an invoice back-dated to yesterday and committed
/// today counts in neither.
/// </para>
/// <para>
/// Two aggregates over the same filtered set, no per-row work, no N+1.
/// </para>
/// </summary>
public sealed class SalesDashboardSummaryQueryHandler(
    VetFlowDbContext dbContext,
    IClinicClock clinicClock)
    : IQueryHandler<SalesDashboardSummaryQuery, SalesDashboardSummaryDto>
{
    public async Task<SalesDashboardSummaryDto> HandleAsync(
        SalesDashboardSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var today = clinicClock.Today;

        var invoices = dbContext.SalesInvoices.AsNoTracking();

        // BR-SAL-003. A draft consumed no stock (BR-SAL-009..012), so it is work outstanding.
        var draftInvoiceCount = await invoices
            .CountAsync(invoice => invoice.Status == SalesInvoiceStatus.Draft, cancellationToken);

        // Committed only: a draft dated today is not a sale, and it is already reported above.
        var todaysInvoices = invoices.Where(invoice =>
            invoice.Status == SalesInvoiceStatus.Committed && invoice.SaleDate == today);

        var todayInvoiceCount = await todaysInvoices.CountAsync(cancellationToken);

        // Each TotalAmount was produced and rounded by Sales when the invoice was committed
        // (DEC-SAL-004); this only adds them up, and the dashboard rounds nothing at all.
        // SUM over an empty set is NULL in SQL, so the coalesce makes "no sales today" a
        // genuine zero rather than a missing value.
        var todayTotal = await todaysInvoices
            .SumAsync(invoice => (decimal?)invoice.TotalAmount, cancellationToken) ?? 0m;

        return new SalesDashboardSummaryDto
        {
            DraftInvoiceCount = draftInvoiceCount,
            TodayInvoiceCount = todayInvoiceCount,
            TodayTotal = new MoneyDto { Amount = todayTotal, Currency = Currencies.EgyptianPound },
        };
    }
}
