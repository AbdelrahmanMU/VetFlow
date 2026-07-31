using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Queries.PurchaseReturnableLines;

/// <summary>
/// The read behind the new-purchase-return screen (REQ-PUR-006, ui.md §مرتجع مشتريات جديد): the
/// lines of one received purchase invoice, each with how much of it remains returnable
/// (BR-PUR-016). Read-only; it never creates a return.
/// </summary>
public sealed record PurchaseReturnableLinesQuery(Guid PurchaseInvoiceId)
    : IQuery<IReadOnlyList<PurchaseReturnableLineDto>?>;
