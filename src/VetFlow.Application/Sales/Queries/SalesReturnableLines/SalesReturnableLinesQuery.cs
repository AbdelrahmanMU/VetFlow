using VetFlow.Application.Common;

namespace VetFlow.Application.Sales.Queries.SalesReturnableLines;

/// <summary>
/// The read behind the new-sales-return screen (REQ-SAL-004, ui.md §مرتجع مبيعات جديد): the lines of
/// one committed sales invoice, each with how much of it remains returnable (BR-SAL-016).
/// Read-only; it never creates a return.
/// </summary>
public sealed record SalesReturnableLinesQuery(Guid SalesInvoiceId)
    : IQuery<IReadOnlyList<SalesReturnableLineDto>?>;
