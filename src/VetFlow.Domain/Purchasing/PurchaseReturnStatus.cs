namespace VetFlow.Domain.Purchasing;

/// <summary>
/// Purchase-return lifecycle status (BR-PUR-018). A return is born a <see cref="Draft"/> and
/// becomes <see cref="Committed"/> exactly once; committed is terminal and the document is then
/// immutable.
///
/// <para>There is deliberately <b>no Cancelled member</b>. A committed return has no reversal
/// path — correction is by an opposing movement (DEC-INV-037) — and a cancelled <i>draft</i> was
/// never ruled for returns, so inventing one here would add a lifecycle nobody approved. The
/// purchase-invoice enum has three values because its own slice ruled all three (BR-PUR-003); this
/// one has two because two is what BR-PUR-018 ruled.</para>
/// </summary>
public enum PurchaseReturnStatus
{
    Draft = 1,
    Committed = 2,
}
