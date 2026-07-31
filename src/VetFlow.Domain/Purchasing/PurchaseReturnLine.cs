using VetFlow.Domain.Common;

namespace VetFlow.Domain.Purchasing;

/// <summary>
/// One line of a purchase return (REQ-PUR-006): a quantity of one product going back to the
/// supplier, bound to the purchase line it originally came in on and to the batch that line
/// created.
///
/// <para><b>The batch is recorded, not chosen.</b> One purchase line creates exactly one batch
/// (DEC-PUR-008), so the destination is a fact of the original document — BR-PUR-017 forbids FEFO
/// or any other selection here. The handler resolves it and passes it in; there is no code path,
/// on the line or anywhere else, that picks a batch for a return.</para>
///
/// <para><b>There is no reason field and no price field.</b> Returns carry no reason code
/// (BR-INV-067 — «مستندها هو سياقها»), and a return has no financial effect at all
/// (DEC-INV-035): it is a stock movement, so a line total would imply a credit that does not
/// exist. Neither is an omission.</para>
///
/// <para>The line is an entity <b>inside</b> the <see cref="PurchaseReturn"/> aggregate — created
/// only through <see cref="PurchaseReturn.AddLine"/>, never on its own (the DEC-PUR-003
/// precedent).</para>
/// </summary>
public sealed class PurchaseReturnLine
{
    private PurchaseReturnLine()
    {
        // EF Core materialization only.
        ProductName = string.Empty;
    }

    internal PurchaseReturnLine(
        Guid id,
        Guid purchaseLineItemId,
        Guid productId,
        string productName,
        Guid batchId,
        decimal quantity,
        DateTimeOffset addedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(purchaseLineItemId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(productId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(batchId, Guid.Empty);
        // Snapshotted by the handler from the original line before the return line is built; an
        // absent name here is a programmer error, not a business failure (the BR-PUR-007 pattern).
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);

        // The aggregate enforces the *shape* of the quantity (BR-PUR-016, first clause). The
        // *ceiling* — the remaining returnable quantity — is deliberately NOT enforced here: it is
        // derived by summing the lines of other committed returns, which the aggregate cannot see
        // without reaching outside itself. The handler owns that check; putting a half-check here
        // would suggest the aggregate guarantees something it cannot.
        if (quantity <= 0)
        {
            throw new BusinessRuleException(
                PurchasingErrorCodes.ReturnLineComposition,
                new Dictionary<string, string> { ["reason"] = "quantityNotPositive" });
        }

        Id = id;
        PurchaseLineItemId = purchaseLineItemId;
        ProductId = productId;
        ProductName = productName;
        BatchId = batchId;
        Quantity = quantity;
        AddedAt = addedAt;
    }

    public Guid Id { get; }

    /// <summary>The original purchase line being returned against (BR-PUR-016).</summary>
    public Guid PurchaseLineItemId { get; private set; }

    public Guid ProductId { get; private set; }

    /// <summary>Immutable product-name snapshot, the BR-PUR-007 pattern.</summary>
    public string ProductName { get; private set; }

    /// <summary>
    /// The batch the quantity returns from — the one created by <see cref="PurchaseLineItemId"/>
    /// (BR-PUR-017, BR-INV-069). Recorded, never selected.
    /// </summary>
    public Guid BatchId { get; private set; }

    /// <summary>Quantity returned, always positive; the sign convention lives in the ledger (BR-INV-064).</summary>
    public decimal Quantity { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }
}
