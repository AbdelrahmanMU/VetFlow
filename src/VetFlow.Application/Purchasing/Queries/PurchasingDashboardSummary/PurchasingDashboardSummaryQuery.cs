using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Queries.PurchasingDashboardSummary;

/// <summary>
/// The purchasing fact the operational dashboard shows (REQ-PUR-007): how many purchase
/// invoices are still drafts — owned by Purchasing, because the dashboard composes reads and
/// computes nothing (BR-DSH-001, DEC-DSH-001). The state is BR-PUR-003's and is not
/// redefined here.
/// <para>
/// <b>Why one count earns a place on a board that rejected twelve candidates:</b> a draft
/// purchase usually means goods that physically arrived and were never recorded. While it
/// stays a draft there is no batch and no on-hand quantity (BR-PUR-009/010 — only receiving
/// touches stock), so <b>every other number on the dashboard is incomplete until it is
/// cleared</b>. This tile corrects the board itself.
/// </para>
/// </summary>
public sealed record PurchasingDashboardSummaryQuery : IQuery<PurchasingDashboardSummaryDto>;
