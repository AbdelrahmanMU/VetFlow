using VetFlow.Domain.Common;

namespace VetFlow.Domain.Sales;

/// <summary>
/// One line of a sales return (REQ-SAL-004): a quantity of one product coming back from the
/// customer, bound to the sale line it originally left on.
///
/// <para><b>There is no BatchId here, and its absence is a rule — not an omission.</b> This is the
/// one place C6 deliberately departs from
/// <see cref="VetFlow.Domain.Purchasing.PurchaseReturnLine"/>, which does carry one, and there are
/// two independent reasons:</para>
/// <list type="number">
///   <item><description><b>One sale line may have left through several batches.</b> FEFO can split
///   a single line across batches (BR-SAL-017), so "the destination" is not one fact the way it is
///   for a purchase line, where one line creates exactly one batch (DEC-PUR-008). A single
///   <c>BatchId</c> could not express the truth.</description></item>
///   <item><description><b>Sales may not hold a batch reference at all</b> (BR-SAL-013,
///   DEC-SAL-006 — «المبيعات لا تقرأ الدفعات، ولا تختارها، ولا تخزّن مرجعًا لها»). Purchasing has no
///   equivalent rule; Sales does, and it is the owner's.</description></item>
/// </list>
///
/// <para>The destination is therefore <b>derived at commit</b> by Inventory from the recorded
/// consumption trace (REQ-INV-008, BR-INV-057), never selected and never FEFO'd (BR-INV-069). What
/// batches actually received the quantity is then visible where it belongs: in the movement ledger,
/// as one <c>SalesReturn</c> row per batch.</para>
///
/// <para><b>There is no reason field and no price field.</b> Returns carry no reason code
/// (BR-INV-067 — «مستندها هو سياقها»), and a return has no financial effect at all (DEC-INV-035):
/// it is a stock movement, so a line total would imply a refund that does not exist — and cash
/// refunds are explicitly still out of scope (DEC-SAL-001). The sale line this one points at does
/// carry a price; that price stays there.</para>
///
/// <para>The line is an entity <b>inside</b> the <see cref="SalesReturn"/> aggregate — created only
/// through <see cref="SalesReturn.AddLine"/>, never on its own (the DEC-PUR-003 precedent, applied
/// to Sales by BR-SAL-004).</para>
/// </summary>
public sealed class SalesReturnLine
{
    private SalesReturnLine()
    {
        // EF Core materialization only.
        ProductName = string.Empty;
    }

    internal SalesReturnLine(
        Guid id,
        Guid salesLineItemId,
        Guid productId,
        string productName,
        decimal quantity,
        bool allowsFractionalQuantity,
        DateTimeOffset addedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(salesLineItemId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(productId, Guid.Empty);
        // Snapshotted by the handler from the original sale line before the return line is built; an
        // absent name here is a programmer error, not a business failure (the BR-SAL-006 pattern).
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        // The aggregate enforces the *shape* of the quantity (BR-SAL-016, first clause). The
        // *ceiling* — the remaining returnable quantity — is deliberately NOT enforced here: it is
        // derived by summing the lines of other committed returns, which the aggregate cannot see
        // without reaching outside itself. The handler owns that check; putting a half-check here
        // would suggest the aggregate guarantees something it cannot.
        if (quantity <= 0)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.ReturnLineComposition,
                new Dictionary<string, string> { ["reason"] = "quantityNotPositive" });
        }

        // BR-SAL-016's last clause: a partial return respects the product's splittability exactly as
        // the sale did (DEC-SAL-007, enforcing REQ-CAT-030 / BR-CAT-032). Returning half of an
        // indivisible item is no more possible than selling half of one — rejected outright, never
        // rounded and never coerced ("no additional rules").
        if (!allowsFractionalQuantity && quantity != decimal.Truncate(quantity))
        {
            throw new BusinessRuleException(
                SalesErrorCodes.ReturnLineComposition,
                new Dictionary<string, string> { ["reason"] = "quantityNotWholeForNonSplittableProduct" });
        }

        Id = id;
        SalesLineItemId = salesLineItemId;
        ProductId = productId;
        ProductName = productName.Trim();
        Quantity = quantity;
        AddedAt = addedAt;
    }

    public Guid Id { get; }

    /// <summary>The original sale line being returned against (BR-SAL-016) — also the anchor Inventory reads the consumption trace by (REQ-INV-008).</summary>
    public Guid SalesLineItemId { get; private set; }

    public Guid ProductId { get; private set; }

    /// <summary>Immutable product-name snapshot, the BR-SAL-006 pattern.</summary>
    public string ProductName { get; private set; }

    /// <summary>
    /// Quantity returned, always positive, expressed in the <b>original sale line's sale unit</b> —
    /// the unit BR-SAL-016 caps and the unit the screen shows. Converting it into stock units is
    /// Inventory's, because only the recorded consumption knows the factor that actually applied.
    /// The sign convention lives in the ledger (BR-INV-064).
    /// </summary>
    public decimal Quantity { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }
}
