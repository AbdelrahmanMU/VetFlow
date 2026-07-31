namespace VetFlow.Domain.Sales;

/// <summary>
/// Sales-return lifecycle status (BR-SAL-018). A return is born a <see cref="Draft"/> and becomes
/// <see cref="Committed"/> exactly once; committed is terminal and the document is then immutable.
///
/// <para>There is deliberately <b>no Cancelled member</b>, exactly as in
/// <see cref="VetFlow.Domain.Purchasing.PurchaseReturnStatus"/>. A committed return has no
/// reversal path — correction is by an opposing movement (DEC-INV-037) — and a cancelled
/// <i>draft</i> was never ruled for returns, so inventing one here would add a lifecycle nobody
/// approved. Note that «ملغاة» on a sales <i>invoice</i> is a different, still-unsettled question
/// (DEC-SAL-009) and gives this enum no third member either.</para>
/// </summary>
public enum SalesReturnStatus
{
    Draft = 1,
    Committed = 2,
}
