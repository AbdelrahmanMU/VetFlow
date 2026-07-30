using VetFlow.Domain.Common;

namespace VetFlow.Domain.Sales;

/// <summary>
/// The sales-invoice aggregate (BR-SAL-001) — the document by which goods leave clinic inventory.
/// Its header carries the system number (BR-SAL-002), the <b>optional</b> free-text customer name
/// (DEC-SAL-002 — the free-text supplier precedent, BR-PUR-001), the required sale date, the
/// status, the derived total, optional notes, and the creation timestamp — plus its
/// <b>line items</b> (BR-SAL-004), the content of the invoice. There is no payment, discount, or
/// tax field anywhere (scope lock, DEC-SAL-001).
///
/// The aggregate is the <b>single</b> owner of the total (BR-SAL-005): <see cref="AddLine"/> and
/// <see cref="RemoveLine"/> mutate the line set and recompute <see cref="TotalAmount"/> as the sum
/// of line totals — no handler, repository, endpoint, database, or frontend ever computes or
/// overwrites it.
///
/// An invoice is always born a <see cref="SalesInvoiceStatus.Draft"/> (BR-SAL-003) and only a draft
/// may change its lines (BR-SAL-004). <see cref="Commit"/> performs the one-time Draft → Committed
/// transition (BR-SAL-009); after it the invoice is fully immutable (BR-SAL-011). The inventory
/// effect is <b>not</b> this aggregate's business: the commit handler asks the Inventory module to
/// consume stock through its public contract, within the same unit of work (BR-SAL-010/013,
/// DEC-SAL-006) — Sales never selects batches and never performs FEFO.
/// </summary>
public sealed class SalesInvoice
{
    private readonly List<SalesLineItem> _lines = [];

    private SalesInvoice()
    {
        // EF Core materialization only.
        Number = string.Empty;
    }

    public SalesInvoice(
        Guid id,
        string number,
        DateOnly saleDate,
        DateTimeOffset createdAt,
        string? customerName = null,
        string? notes = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        // The system number is generated at persist time (BR-SAL-002); an absent number here is a
        // programmer error, not a business failure — the SAL- format itself is owned by Infrastructure.
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentOutOfRangeException.ThrowIfEqual(saleDate, default);

        Id = id;
        Number = number;
        // Optional free text (DEC-SAL-002): blank and absent are the same thing — null.
        CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName.Trim();
        SaleDate = saleDate;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = createdAt;
        Status = SalesInvoiceStatus.Draft;
        TotalAmount = 0m;
    }

    public Guid Id { get; }

    /// <summary>
    /// System-generated internal number (BR-SAL-002): the fixed prefix <c>SAL-</c> plus a unique
    /// ascending sequence value (<c>SAL-000001</c>). Assigned once at first persist and never
    /// changed, never reused; the invoice's stable identity even if the customer field is later
    /// replaced by a Customers module (DEC-SAL-002).
    /// </summary>
    public string Number { get; private set; }

    /// <summary>Free-text customer name, <b>optional</b> (BR-SAL-001, DEC-SAL-002); no Customers module yet.</summary>
    public string? CustomerName { get; private set; }

    /// <summary>The sale date (BR-SAL-001) — required; a business date, no time component.</summary>
    public DateOnly SaleDate { get; private set; }

    /// <summary>Header total in Egyptian Pounds — always derived from the lines (BR-SAL-005).</summary>
    public decimal TotalAmount { get; private set; }

    /// <summary>Optional free-text notes; never part of search (BR-SAL-001).</summary>
    public string? Notes { get; private set; }

    /// <summary>System timestamp recorded when the invoice is first saved (BR-SAL-001).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    public SalesInvoiceStatus Status { get; private set; }

    /// <summary>The invoice's line items (BR-SAL-004); a draft may temporarily hold none.</summary>
    public IReadOnlyCollection<SalesLineItem> Lines => _lines.AsReadOnly();

    /// <summary>
    /// Add a line to a draft invoice (REQ-SAL-001, BR-SAL-004) and recompute the total
    /// (BR-SAL-005). The product name, sale-unit name, and sale price are already resolved to
    /// their catalog snapshot by the handler (BR-SAL-006, DEC-SAL-003), as is
    /// <paramref name="allowsFractionalQuantity"/> — the product's <c>IsSplittable</c> capability
    /// the line enforces (DEC-SAL-007). Only a draft may change (BR-SAL-003/011); otherwise the
    /// call is rejected without mutation. There is no edit path: correcting a line is remove then
    /// add (BR-SAL-004).
    /// </summary>
    public SalesLineItem AddLine(
        Guid lineId,
        Guid productId,
        string productName,
        Guid saleUnitId,
        string saleUnitName,
        decimal quantity,
        decimal unitPrice,
        bool allowsFractionalQuantity,
        DateTimeOffset addedAt)
    {
        EnsureDraft();

        var line = new SalesLineItem(
            lineId,
            productId,
            productName,
            saleUnitId,
            saleUnitName,
            quantity,
            unitPrice,
            allowsFractionalQuantity,
            addedAt);

        _lines.Add(line);
        RecalculateTotal();
        return line;
    }

    /// <summary>
    /// Remove a line from a draft invoice by id (REQ-SAL-001, BR-SAL-004) and recompute the total
    /// (BR-SAL-005). Returns <c>false</c> when the line is not on this invoice (the endpoint
    /// answers 404) — the total is unchanged in that case. Only a draft may change (BR-SAL-003).
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
    /// Commit a draft invoice (REQ-SAL-003, BR-SAL-009): the one-time, all-or-nothing transition
    /// Draft → Committed (BR-SAL-003). Only a draft with at least one line may be committed;
    /// committing a non-draft invoice or an empty one is rejected without mutation (BR-SAL-012).
    /// The inventory effect — FEFO allocation, the batch decrements, the on-hand decrease, and the
    /// traceability record — is applied by the commit handler through the Inventory consumption
    /// contract within the same unit of work (BR-SAL-010, DEC-SAL-006); this method owns only the
    /// state transition. Afterwards the invoice is immutable (BR-SAL-011): the Draft-only guards on
    /// <see cref="AddLine"/> / <see cref="RemoveLine"/> block any further change, and a second
    /// commit is rejected here.
    /// </summary>
    public void Commit()
    {
        EnsureDraft();

        if (_lines.Count == 0)
        {
            throw new BusinessRuleException(SalesErrorCodes.InvoiceHasNoLines);
        }

        Status = SalesInvoiceStatus.Committed;
    }

    private void EnsureDraft()
    {
        if (Status != SalesInvoiceStatus.Draft)
        {
            throw new BusinessRuleException(SalesErrorCodes.InvoiceNotDraft);
        }
    }

    /// <summary>
    /// The single canonical total computation (BR-SAL-005): the sum of the already-rounded line
    /// totals (BR-SAL-007). An invoice with no lines totals zero.
    /// </summary>
    private void RecalculateTotal() => TotalAmount = _lines.Sum(line => line.LineTotal);
}
