namespace VetFlow.Application.Purchasing.Queries.PurchasingDashboardSummary;

/// <summary>
/// Purchasing's answer to the dashboard (REQ-PUR-007, BR-PUR-019).
/// </summary>
public sealed record PurchasingDashboardSummaryDto
{
    /// <summary>
    /// Purchase invoices in <c>Draft</c> (BR-PUR-003). Received and cancelled invoices are
    /// not counted — both are terminal states with no work outstanding.
    /// <para>
    /// <b>A count, with no money beside it.</b> DEC-DSH-012 allows exactly one monetary figure
    /// on the whole dashboard, and it is today's sales total.
    /// </para>
    /// </summary>
    public required int DraftInvoiceCount { get; init; }
}
