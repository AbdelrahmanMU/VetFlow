using VetFlow.Domain.Common;

namespace VetFlow.Domain.Purchasing;

/// <summary>
/// One line of a purchase invoice (BR-PUR-005): a Catalog product bought in one of
/// its purchase units, at a user-entered quantity and unit price. The product and
/// unit are referenced by id, and their names are captured as an <b>immutable
/// snapshot</b> at add time (BR-PUR-007) — a purchase invoice is a historical
/// document, unaffected by later catalog edits. The line total is
/// quantity × unit price, rounded once here to EGP granularity so the invoice total
/// (the sum of line totals) always equals the sum of the displayed line totals
/// (BR-PUR-006, AC-PUR-011). The line is an entity <b>inside</b> the
/// <see cref="PurchaseInvoice"/> aggregate — it is created only through
/// <see cref="PurchaseInvoice.AddLine"/> and never exists on its own (DEC-PUR-003).
/// </summary>
public sealed class PurchaseLineItem
{
    private PurchaseLineItem()
    {
        // EF Core materialization only.
        ProductName = string.Empty;
        PurchaseUnitName = string.Empty;
    }

    internal PurchaseLineItem(
        Guid id,
        Guid productId,
        string productName,
        Guid purchaseUnitId,
        string purchaseUnitName,
        decimal quantity,
        decimal unitPrice,
        DateTimeOffset addedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(productId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(purchaseUnitId, Guid.Empty);
        // The names are resolved (snapshotted) by the handler from the catalog before
        // the line is built; an absent name here is a programmer error, not a business
        // failure (BR-PUR-007).
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentException.ThrowIfNullOrWhiteSpace(purchaseUnitName);

        // Quantity and unit price are validated field-by-field by the command validator
        // (AC-PUR-009); the aggregate re-enforces them as the backstop (STD-BE-010).
        if (quantity <= 0)
        {
            throw new BusinessRuleException(
                PurchasingErrorCodes.LineComposition,
                new Dictionary<string, string> { ["reason"] = "quantityNotPositive" });
        }

        if (unitPrice < 0)
        {
            throw new BusinessRuleException(
                PurchasingErrorCodes.LineComposition,
                new Dictionary<string, string> { ["reason"] = "unitPriceNegative" });
        }

        Id = id;
        ProductId = productId;
        ProductName = productName.Trim();
        PurchaseUnitId = purchaseUnitId;
        PurchaseUnitName = purchaseUnitName.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        // The single point the line total is computed (BR-PUR-005). Rounded to EGP so the
        // in-memory sum equals the persisted, reloaded, per-line sum (BR-PUR-006).
        LineTotal = Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
        AddedAt = addedAt;
    }

    public Guid Id { get; }

    /// <summary>The referenced Catalog product (kept for future linkage; display uses the snapshot).</summary>
    public Guid ProductId { get; }

    /// <summary>Immutable product-name snapshot captured at add time (BR-PUR-007).</summary>
    public string ProductName { get; }

    /// <summary>The referenced purchase unit of the product (BR-PUR-005).</summary>
    public Guid PurchaseUnitId { get; }

    /// <summary>Immutable purchase-unit-name snapshot captured at add time (BR-PUR-007).</summary>
    public string PurchaseUnitName { get; }

    public decimal Quantity { get; }

    /// <summary>User-entered unit price in EGP (BR-PUR-005).</summary>
    public decimal UnitPrice { get; }

    /// <summary>Quantity × unit price, rounded to EGP (BR-PUR-005) — derived, never entered.</summary>
    public decimal LineTotal { get; }

    /// <summary>System timestamp at add time; orders the lines deterministically (not displayed).</summary>
    public DateTimeOffset AddedAt { get; }
}
