using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Queries.SalesDashboardSummary;

/// <summary>
/// The sales facts the operational dashboard shows (REQ-SAL-006): the draft-invoice count,
/// and today's committed invoice count and total — in <b>one</b> read owned by Sales.
/// <para>
/// <b>It exists here because the Dashboard may not compute it</b> (BR-DSH-001, DEC-DSH-001):
/// the dashboard composes reads its modules own and calculates nothing. Nothing is redefined
/// here either — the states are BR-SAL-003's and the money rounding is DEC-SAL-004's.
/// </para>
/// <para>
/// <b>"Today" is the clinic local date</b>, from the single cross-cutting source
/// (<c>docs/architecture/cross-cutting/clinic-date.md</c>, owner ruling OQ-DSH-2). UTC,
/// server time and the caller's device time are all prohibited, and there is no fallback.
/// </para>
/// <para>
/// <b>The reference field is the header's sale date, not the commit time.</b> That is the
/// field BR-SAL-019 filters and sorts by, so this number and the sales list agree
/// (BR-DSH-018). Taking the commit time instead would make an invoice back-dated to yesterday
/// count here but not appear on the screen this tile links to — a contradiction visible to
/// the owner with nothing to justify it.
/// </para>
/// </summary>
public sealed record SalesDashboardSummaryQuery : IQuery<SalesDashboardSummaryDto>;
