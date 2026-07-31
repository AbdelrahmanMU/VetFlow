using VetFlow.Domain.Common;

namespace VetFlow.Domain.Purchasing;

/// <summary>
/// The purchase-return aggregate (REQ-PUR-006, DEC-PUR-010) — the document by which received goods
/// go back to the supplier. Built on the existing invoice pattern by the owner's ruling: header +
/// lines + a one-time Draft → Committed transition, mirroring <see cref="PurchaseInvoice"/>.
///
/// <para>Its header carries the system number (BR-PUR-014, <c>PRT-</c>), the <b>single</b>
/// originating purchase invoice (BR-PUR-015 — one original invoice per return, DEC-PUR-010), the
/// supplier-name snapshot taken from that invoice, the required return date, the status, optional
/// notes, and the creation timestamp.</para>
///
/// <para><b>What this aggregate deliberately does not own.</b> Three rules that govern returns live
/// outside it, and each is outside for a reason:</para>
/// <list type="bullet">
///   <item><description><b>BR-PUR-015</b> (the original invoice must be Received) — needs the
///   invoice, which is a different aggregate. The handler checks it before constructing this one,
///   so an illegal return is never built at all.</description></item>
///   <item><description><b>BR-PUR-016</b> (the returnable ceiling) — derived by summing the lines
///   of other <i>committed</i> returns. Drafts never reserve, which is a deliberate choice with a
///   visible consequence: two drafts can each pass validation and the second fails at commit.
///   </description></item>
///   <item><description><b>BR-PUR-018</b>'s stock effect — decrementing the batch, the on-hand
///   quantity and writing the ledger row happen through the Inventory contract in the same unit of
///   work (BR-INV-062, DEC-INV-019). <see cref="Commit"/> owns only the state transition, exactly
///   as <see cref="SalesInvoice"/>'s does.</description></item>
/// </list>
///
/// <para>There is <b>no total</b>: a return has no financial effect whatsoever (DEC-INV-035). There
/// is <b>no reason</b>: returns carry none (BR-INV-067). There is <b>no cancellation</b>: a
/// committed return is corrected by an opposing movement (DEC-INV-037).</para>
/// </summary>
public sealed class PurchaseReturn
{
    private readonly List<PurchaseReturnLine> _lines = [];

    private PurchaseReturn()
    {
        // EF Core materialization only.
        Number = string.Empty;
    }

    public PurchaseReturn(
        Guid id,
        string number,
        Guid purchaseInvoiceId,
        string? supplierName,
        DateOnly returnDate,
        DateTimeOffset createdAt,
        string? notes = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(purchaseInvoiceId, Guid.Empty);
        // Generated at persist time from the sequence (BR-PUR-014); an absent number here is a
        // programmer error. The PRT- format itself is owned by Infrastructure, like PUR- and SAL-.
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentOutOfRangeException.ThrowIfEqual(returnDate, default);

        Id = id;
        Number = number;
        PurchaseInvoiceId = purchaseInvoiceId;
        // Snapshot of the original invoice's supplier, which is itself optional free text
        // (BR-PUR-001) — so blank and absent are the same thing here too.
        SupplierName = string.IsNullOrWhiteSpace(supplierName) ? null : supplierName.Trim();
        ReturnDate = returnDate;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = createdAt;
        Status = PurchaseReturnStatus.Draft;
    }

    public Guid Id { get; }

    /// <summary>
    /// System-generated number (BR-PUR-014): the fixed prefix <c>PRT-</c> plus a unique ascending
    /// sequence value (<c>PRT-000001</c>). Assigned once at first persist, never changed, never
    /// reused — and visibly distinct from <c>PUR-</c> so a return is never misread as an invoice.
    /// </summary>
    public string Number { get; private set; }

    /// <summary>The one originating purchase invoice (BR-PUR-015, DEC-PUR-010) — never more than one.</summary>
    public Guid PurchaseInvoiceId { get; private set; }

    /// <summary>Supplier-name snapshot from the original invoice; optional, as suppliers are free text (BR-PUR-001).</summary>
    public string? SupplierName { get; private set; }

    /// <summary>The return date — required; a business date, no time component.</summary>
    public DateOnly ReturnDate { get; private set; }

    /// <summary>Optional free-text notes.</summary>
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public PurchaseReturnStatus Status { get; private set; }

    /// <summary>The return's lines (REQ-PUR-006); a draft may temporarily hold none.</summary>
    public IReadOnlyCollection<PurchaseReturnLine> Lines => _lines.AsReadOnly();

    /// <summary>
    /// Add a line to a draft return (REQ-PUR-006, BR-PUR-016). The product-name snapshot and the
    /// <paramref name="batchId"/> are resolved by the handler from the original purchase line —
    /// the batch is a fact of that line, not a choice (BR-PUR-017). Only a draft may change
    /// (BR-PUR-018); otherwise the call is rejected without mutation. There is no edit path:
    /// correcting a line is remove then add, the BR-PUR-005 precedent.
    /// </summary>
    public PurchaseReturnLine AddLine(
        Guid lineId,
        Guid purchaseLineItemId,
        Guid productId,
        string productName,
        Guid batchId,
        decimal quantity,
        DateTimeOffset addedAt)
    {
        EnsureDraft();

        var line = new PurchaseReturnLine(
            lineId,
            purchaseLineItemId,
            productId,
            productName,
            batchId,
            quantity,
            addedAt);

        _lines.Add(line);
        return line;
    }

    /// <summary>
    /// Remove a line from a draft return by id (BR-PUR-018). Returns <c>false</c> when the line is
    /// not on this return (the endpoint answers 404). Only a draft may change.
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
        return true;
    }

    /// <summary>
    /// Commit a draft return (BR-PUR-018): the one-time, all-or-nothing transition Draft →
    /// Committed. Only a draft with at least one line may be committed; committing a non-draft or
    /// an empty return is rejected without mutation.
    ///
    /// <para>The inventory effect — the batch decrement, the on-hand decrement and one ledger row
    /// per line — is applied by the commit handler in the same unit of work (BR-INV-062); this
    /// method owns only the state transition. Afterwards the document is immutable (BR-PUR-018):
    /// the Draft-only guards block every further change and a second commit is rejected here.</para>
    /// </summary>
    public void Commit()
    {
        EnsureDraft();

        if (_lines.Count == 0)
        {
            throw new BusinessRuleException(PurchasingErrorCodes.ReturnHasNoLines);
        }

        Status = PurchaseReturnStatus.Committed;
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseReturnStatus.Draft)
        {
            throw new BusinessRuleException(PurchasingErrorCodes.ReturnNotDraft);
        }
    }
}
