using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Queries.PurchasingDashboardSummary;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Purchasing's dashboard read (REQ-PUR-007, BR-PUR-019). One count, read-only, owning no
/// state and changing no invoice.
/// <para>
/// Drafts only (BR-PUR-003). <c>Received</c> and <c>Cancelled</c> are terminal states with no
/// outstanding work, so neither is counted — and neither appears on the destination screen
/// under the draft filter (BR-PUR-004), which is what AC-PUR-027 pins by test.
/// </para>
/// </summary>
public sealed class PurchasingDashboardSummaryQueryHandler(VetFlowDbContext dbContext)
    : IQueryHandler<PurchasingDashboardSummaryQuery, PurchasingDashboardSummaryDto>
{
    public async Task<PurchasingDashboardSummaryDto> HandleAsync(
        PurchasingDashboardSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var draftInvoiceCount = await dbContext.PurchaseInvoices
            .AsNoTracking()
            .CountAsync(invoice => invoice.Status == PurchaseInvoiceStatus.Draft, cancellationToken);

        return new PurchasingDashboardSummaryDto { DraftInvoiceCount = draftInvoiceCount };
    }
}
