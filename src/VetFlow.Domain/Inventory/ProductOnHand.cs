namespace VetFlow.Domain.Inventory;

/// <summary>
/// The persisted on-hand quantity of one product (BR-INV-002, write-kernel.md), in the product's
/// canonical stock unit. Receiving increments it atomically as batches are created (owner ruling
/// 2026-07-22 — a stored value, never derived from batches in MVP, BR-INV-004). Exactly one row
/// per product; the product id is its identity.
/// </summary>
public sealed class ProductOnHand
{
    private ProductOnHand()
    {
        // EF Core materialization only.
    }

    public ProductOnHand(Guid productId)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(productId, Guid.Empty);

        ProductId = productId;
        OnHandQuantity = 0m;
    }

    /// <summary>Identity: one on-hand record per product.</summary>
    public Guid ProductId { get; }

    /// <summary>On-hand quantity in the product's canonical stock unit (BR-INV-002).</summary>
    public decimal OnHandQuantity { get; private set; }

    /// <summary>Increase the on-hand quantity by a received amount in the stock unit (BR-INV-002).</summary>
    public void Increase(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        OnHandQuantity += quantity;
    }

    /// <summary>
    /// Decrease the on-hand quantity by a consumed amount in the stock unit (BR-INV-047) — the
    /// mirror of <see cref="Increase"/>, and the second and last inventory movement (BR-INV-055).
    /// The amount must equal exactly the total decremented from the product's batches, which keeps
    /// the BR-INV-005 invariant true (BR-INV-049). Consumption never drives the balance negative:
    /// sufficiency is checked against the saleable batches before anything is written
    /// (BR-INV-052), so a negative result here is a programmer error, not a business failure.
    /// </summary>
    public void Decrease(decimal quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(quantity, OnHandQuantity);

        OnHandQuantity -= quantity;
    }
}
