using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Queries.SalesDashboardSummary;

/// <summary>
/// Sales' answer to the dashboard (REQ-SAL-006, BR-SAL-020). Primitives only.
/// </summary>
public sealed record SalesDashboardSummaryDto
{
    /// <summary>
    /// Invoices still in <c>Draft</c> (BR-SAL-003). A draft sale consumed no stock
    /// (BR-SAL-009..012), so it is revenue not recorded and stock that only looks present.
    /// </summary>
    public required int DraftInvoiceCount { get; init; }

    /// <summary>
    /// <b>Committed</b> invoices whose header sale date is the clinic local date. Drafts are
    /// excluded — they are not sales yet, and they are counted in
    /// <see cref="DraftInvoiceCount"/>.
    /// </summary>
    public required int TodayInvoiceCount { get; init; }

    /// <summary>
    /// The sum of those invoices' totals. Each total was produced and rounded by Sales when
    /// the invoice was committed (DEC-SAL-004 — half away from zero, two places); <b>the
    /// dashboard neither sums nor rounds anything</b>. Zero when there are none.
    /// <para>
    /// Carried as <see cref="MoneyDto"/>, like every other amount the API returns, so the
    /// currency travels with the number and no screen has to assume one.
    /// </para>
    /// </summary>
    public required MoneyDto TodayTotal { get; init; }
}
