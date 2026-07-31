namespace VetFlow.Application.Sales.Queries.SalesList;

/// <summary>
/// The status list filter (BR-SAL-019); absent means both states. Two members
/// only — the sales state machine has no Cancelled (BR-SAL-003, DEC-SAL-009
/// open and not invented).
/// </summary>
public enum SalesInvoiceStatusFilter
{
    Draft = 1,
    Committed = 2,
}
