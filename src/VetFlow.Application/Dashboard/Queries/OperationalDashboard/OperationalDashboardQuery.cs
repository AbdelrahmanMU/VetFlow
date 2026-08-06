using VetFlow.Application.Common;

namespace VetFlow.Application.Dashboard.Queries.OperationalDashboard;

/// <summary>
/// The whole operational dashboard in one read (REQ-DSH-010, لوحة التشغيل).
/// <para>
/// <b>It takes no parameters, and that is a rule rather than an omission</b> (BR-DSH-020): the
/// board is not filtered, sorted, paged or personalised. Its entire scope comes from the
/// access token (tenant and branch) and from the clinic local date — nothing a caller can
/// influence.
/// </para>
/// <para>
/// <b>This module owns no business fact and computes none</b> (BR-DSH-001, DEC-DSH-001,
/// reaffirmed by the owner on 2026-08-03). Every number is produced by the module that owns
/// its rule — Inventory (REQ-INV-013), Sales (REQ-SAL-006), Purchasing (REQ-PUR-007) — and
/// the dashboard only arranges them. That is why nothing in
/// <c>VetFlow.Application.Dashboard</c> references another module: the composition happens in
/// Infrastructure, the sanctioned cross-module read path (ADR-0014 §2), and the module
/// isolation test enforces it.
/// </para>
/// </summary>
public sealed record OperationalDashboardQuery : IQuery<OperationalDashboardDto>;
