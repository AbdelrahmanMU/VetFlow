using VetFlow.Domain.Common;

namespace VetFlow.Domain.Purchasing;

/// <summary>
/// The purchase-invoice aggregate — the document by which goods enter inventory
/// from a supplier (purchasing overview). It carries the header (BR-PUR-001):
/// identity, the free-text supplier name, the optional supplier reference, the
/// invoice date, the status, the total, optional notes, and the creation
/// timestamp — plus its <b>line items</b> (BR-PUR-005), the content of the
/// invoice. Receiving and the inventory effect belong to later slices (scope
/// lock, overview.md).
///
/// The aggregate is the <b>single</b> owner of the total (BR-PUR-006, DEC-PUR-003):
/// <see cref="AddLine"/> and <see cref="RemoveLine"/> mutate the line set and
/// recompute <see cref="TotalAmount"/> as the sum of line totals — no handler,
/// repository, endpoint, database, or frontend ever computes or overwrites it.
///
/// An invoice is always born a <see cref="PurchaseInvoiceStatus.Draft"/>
/// (BR-PUR-003) and only a draft may change its lines (BR-PUR-005). The
/// received/cancelled transitions and their inventory effect are owned by the
/// receiving slice, so — as with the Catalog deactivation workflow — no transition
/// method exists yet; a non-draft state is a persisted fact set by seeding, never
/// produced by this aggregate.
/// </summary>
public sealed class PurchaseInvoice
{
    private readonly List<PurchaseLineItem> _lines = [];

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

    /// <summary>The invoice's line items (BR-PUR-005); an invoice may temporarily hold none.</summary>
    public IReadOnlyCollection<PurchaseLineItem> Lines => _lines.AsReadOnly();

    /// <summary>
    /// Add a line to a draft invoice (REQ-PUR-004, BR-PUR-005) and recompute the total
    /// (BR-PUR-006). The product and unit names are already resolved to their catalog
    /// snapshot by the handler (BR-PUR-007). Only a draft may change (BR-PUR-003);
    /// otherwise the call is rejected without mutation.
    /// </summary>
    public PurchaseLineItem AddLine(
        Guid lineId,
        Guid productId,
        string productName,
        Guid purchaseUnitId,
        string purchaseUnitName,
        decimal quantity,
        decimal unitPrice,
        DateTimeOffset addedAt)
    {
        EnsureDraft();

        var line = new PurchaseLineItem(
            lineId,
            productId,
            productName,
            purchaseUnitId,
            purchaseUnitName,
            quantity,
            unitPrice,
            addedAt);

        _lines.Add(line);
        RecalculateTotal();
        return line;
    }

    /// <summary>
    /// Remove a line from a draft invoice by id (REQ-PUR-004, BR-PUR-005) and recompute
    /// the total (BR-PUR-006). Returns <c>false</c> when the line is not on this invoice
    /// (the endpoint answers 404) — the total is unchanged in that case. Only a draft may
    /// change (BR-PUR-003).
    /// </summary>
    public bool RemoveLine(Guid lineId)
    {
        EnsureDraft();

        var line = _lines.FirstOrDefault(item => item.Id == lineId);
        if (line is null)
        {
            return false;
        }

        _lines.Remove(line);
        RecalculateTotal();
        return true;
    }

    /// <summary>
    /// Receive a draft invoice (REQ-PUR-005, BR-PUR-009): the one-time, all-or-nothing
    /// transition Draft → Received (BR-PUR-003). Only a draft with at least one line may be
    /// received (BR-PUR-012); receiving a non-draft invoice or an empty one is rejected
    /// without mutation. The inventory effect — one batch per line and the on-hand increase
    /// — is applied by the receiving handler through the Inventory write kernel within the
    /// same unit of work (BR-PUR-010, DEC-PUR-008); this method owns only the state
    /// transition. Afterwards the invoice is immutable (BR-PUR-011): the Draft-only guards on
    /// <see cref="AddLine"/> / <see cref="RemoveLine"/> block any further change.
    /// </summary>
    public void Receive()
    {
        EnsureDraft();

        if (_lines.Count == 0)
        {
            throw new BusinessRuleException(PurchasingErrorCodes.InvoiceHasNoLines);
        }

        Status = PurchaseInvoiceStatus.Received;
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseInvoiceStatus.Draft)
        {
            throw new BusinessRuleException(PurchasingErrorCodes.InvoiceNotDraft);
        }
    }

    /// <summary>
    /// The single canonical total computation (BR-PUR-006, DEC-PUR-003): the sum of the
    /// (already EGP-rounded) line totals. An invoice with no lines totals zero.
    /// </summary>
    private void RecalculateTotal() => TotalAmount = _lines.Sum(line => line.LineTotal);
}
