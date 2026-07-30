using VetFlow.Domain.Common;

namespace VetFlow.Domain.Sales;

/// <summary>
/// One line of a sales invoice (BR-SAL-004): a Catalog product sold in one of its sale units, at
/// a user-entered quantity and the <b>catalog sale price captured as a snapshot</b> at add time —
/// the price is never entered or edited in Sprint 7 (DEC-SAL-003; no discount, no override, no
/// reason, no audit). The product and unit are referenced by id and their names are captured as an
/// immutable snapshot too (BR-SAL-006): a sales invoice is a historical document, unaffected by
/// later catalog edits. The line total is quantity × unit price, rounded once here half away from
/// zero to two decimals (BR-SAL-007), so the invoice total always equals the sum of the displayed
/// line totals (BR-SAL-005). The line is an entity <b>inside</b> the <see cref="SalesInvoice"/>
/// aggregate — created only through <see cref="SalesInvoice.AddLine"/>, never on its own
/// (the DEC-PUR-003 precedent).
///
/// The line carries <b>no batch information of any kind</b> (BR-SAL-013): Sales expresses intent,
/// Inventory owns allocation.
/// </summary>
public sealed class SalesLineItem
{
    private SalesLineItem()
    {
        // EF Core materialization only.
        ProductName = string.Empty;
        SaleUnitName = string.Empty;
    }

    internal SalesLineItem(
        Guid id,
        Guid productId,
        string productName,
        Guid saleUnitId,
        string saleUnitName,
        decimal quantity,
        decimal unitPrice,
        bool allowsFractionalQuantity,
        DateTimeOffset addedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(productId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(saleUnitId, Guid.Empty);
        // The names are resolved (snapshotted) by the handler from the catalog before the line is
        // built; an absent name here is a programmer error, not a business failure (BR-SAL-006).
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentException.ThrowIfNullOrWhiteSpace(saleUnitName);

        // Quantity and price are validated field-by-field by the command validator (AC-SAL-003);
        // the aggregate re-enforces them as the backstop (STD-BE-010).
        if (quantity <= 0)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.LineComposition,
                new Dictionary<string, string> { ["reason"] = "quantityNotPositive" });
        }

        if (unitPrice < 0)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.LineComposition,
                new Dictionary<string, string> { ["reason"] = "unitPriceNegative" });
        }

        // Partial-selling constraint (DEC-SAL-007 — enforcement of the existing REQ-CAT-030 /
        // BR-CAT-032): a product that is not splittable may only be sold in whole units. Explicit
        // rejection — no automatic rounding, no coercion, no exception path ("no additional rules").
        if (!allowsFractionalQuantity && quantity != decimal.Truncate(quantity))
        {
            throw new BusinessRuleException(
                SalesErrorCodes.LineComposition,
                new Dictionary<string, string> { ["reason"] = "quantityNotWholeForNonSplittableProduct" });
        }

        Id = id;
        ProductId = productId;
        ProductName = productName.Trim();
        SaleUnitId = saleUnitId;
        SaleUnitName = saleUnitName.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        // The single point the line total is computed (BR-SAL-004). Rounded once, half away from
        // zero, to exactly two decimals (BR-SAL-007 — banker's rounding forbidden), so the
        // in-memory sum equals the persisted, reloaded, per-line sum (BR-SAL-005).
        LineTotal = Math.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero);
        AddedAt = addedAt;
    }

    public Guid Id { get; }

    /// <summary>The referenced Catalog product; display uses the snapshot below.</summary>
    public Guid ProductId { get; }

    /// <summary>Immutable product-name snapshot captured at add time (BR-SAL-006).</summary>
    public string ProductName { get; }

    /// <summary>The referenced sale unit of the product (BR-SAL-004).</summary>
    public Guid SaleUnitId { get; }

    /// <summary>Immutable sale-unit-name snapshot captured at add time (BR-SAL-006).</summary>
    public string SaleUnitName { get; }

    /// <summary>Quantity in the chosen sale unit — never rounded (BR-INV-058).</summary>
    public decimal Quantity { get; }

    /// <summary>Immutable snapshot of the catalog sale price for the chosen unit (BR-SAL-004/006, DEC-SAL-003).</summary>
    public decimal UnitPrice { get; }

    /// <summary>Quantity × unit price, rounded to two decimals (BR-SAL-007) — derived, never entered.</summary>
    public decimal LineTotal { get; }

    /// <summary>System timestamp at add time; orders the lines deterministically (not displayed).</summary>
    public DateTimeOffset AddedAt { get; }
}
