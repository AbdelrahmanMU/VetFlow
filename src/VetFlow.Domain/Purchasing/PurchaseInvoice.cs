namespace VetFlow.Domain.Purchasing;

/// <summary>
/// The purchase-invoice aggregate — the document by which goods enter inventory
/// from a supplier (purchasing overview). In this first version it is a
/// <b>header only</b> (BR-PUR-001): identity, the free-text supplier name, the
/// optional supplier reference, the invoice date, the status, the header total,
/// optional notes, and the creation timestamp. Line items, cost, receiving, and
/// the inventory effect belong to later slices (scope lock, overview.md).
///
/// An invoice is always born a <see cref="PurchaseInvoiceStatus.Draft"/>
/// (BR-PUR-003). The received/cancelled transitions and their inventory effect
/// are owned by the receiving slice, so — as with the Catalog deactivation
/// workflow — no transition method exists yet; a non-draft state is a persisted
/// fact set by seeding, never produced by this aggregate.
/// </summary>
public sealed class PurchaseInvoice
{
    private PurchaseInvoice()
    {
        // EF Core materialization only.
        Number = string.Empty;
        SupplierName = string.Empty;
    }

    public PurchaseInvoice(
        Guid id,
        string number,
        string supplierName,
        DateOnly invoiceDate,
        decimal totalAmount,
        DateTimeOffset createdAt,
        string? supplierInvoiceReference = null,
        string? notes = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        // The system number is generated at persist time (BR-PUR-002); an absent
        // number here is a programmer error, not a business failure — the PUR-
        // format itself is owned by Infrastructure.
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        // Supplier name is a required free-text field (BR-PUR-001). Create-time
        // input validation lands with the creation slice; the aggregate simply
        // refuses to exist without it (STD-BE-012).
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierName);
        ArgumentOutOfRangeException.ThrowIfEqual(invoiceDate, default);
        ArgumentOutOfRangeException.ThrowIfNegative(totalAmount);

        Id = id;
        Number = number;
        SupplierName = supplierName.Trim();
        SupplierInvoiceReference = string.IsNullOrWhiteSpace(supplierInvoiceReference)
            ? null
            : supplierInvoiceReference.Trim();
        InvoiceDate = invoiceDate;
        TotalAmount = totalAmount;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = createdAt;
        Status = PurchaseInvoiceStatus.Draft;
    }

    public Guid Id { get; }

    /// <summary>
    /// System-generated internal number (BR-PUR-002): the fixed prefix <c>PUR-</c>
    /// plus a unique ascending sequence value (<c>PUR-000001</c>). Assigned once at
    /// first persist and never changed, never reused; the invoice's stable identity
    /// even if the supplier field is later replaced by a Suppliers module (BR-PUR-001).
    /// </summary>
    public string Number { get; private set; }

    /// <summary>Free-text supplier name, required (BR-PUR-001); no Suppliers module yet.</summary>
    public string SupplierName { get; private set; }

    /// <summary>The supplier's own invoice reference number, optional free text (BR-PUR-001).</summary>
    public string? SupplierInvoiceReference { get; private set; }

    /// <summary>The supplier-stated invoice date (BR-PUR-001); a business date, no time component.</summary>
    public DateOnly InvoiceDate { get; private set; }

    /// <summary>
    /// Header total in Egyptian Pounds (BR-PUR-001). Derived from line items once the
    /// creation slice defines them; a header-only invoice carries the amount directly.
    /// </summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>Optional free-text notes; never part of list search (BR-PUR-004).</summary>
    public string? Notes { get; private set; }

    /// <summary>System timestamp recorded when the invoice is first saved (BR-PUR-001); shown in the list.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    public PurchaseInvoiceStatus Status { get; private set; }
}
