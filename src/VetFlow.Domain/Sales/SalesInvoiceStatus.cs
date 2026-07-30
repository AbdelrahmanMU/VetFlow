namespace VetFlow.Domain.Sales;

/// <summary>
/// The sales-invoice state machine (BR-SAL-003). Exactly two states: an invoice is born a
/// <see cref="Draft"/> — no inventory effect whatsoever — and commits once to
/// <see cref="Committed"/>, a terminal state in which the stock has been consumed. The only
/// permitted transition is Draft → Committed; Committed → Draft, a repeated commit, a partial
/// commit, and undo are all forbidden. A "cancelled" state was deliberately not introduced
/// (DEC-SAL-009 — undecided, not invented).
/// </summary>
public enum SalesInvoiceStatus
{
    Draft = 0,
    Committed = 1,
}
