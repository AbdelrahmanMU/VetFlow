using VetFlow.Domain.Common;

namespace VetFlow.Domain.Sales;

/// <summary>
/// The sales-return aggregate (REQ-SAL-004, DEC-SAL-010) — the document by which a customer's goods
/// come back into stock. Built on the existing invoice pattern by the owner's ruling: header +
/// lines + a one-time Draft → Committed transition, mirroring <see cref="SalesInvoice"/>.
///
/// <para>Its header carries the system number (BR-SAL-014, <c>SRT-</c>), the <b>single</b>
/// originating sales invoice (BR-SAL-015 — one original invoice per return, DEC-SAL-010), the
/// customer-name snapshot taken from that invoice (optional, because the customer itself is
/// optional free text — DEC-SAL-002), the required return date, the status, optional notes, and the
/// creation timestamp.</para>
///
/// <para><b>What this aggregate deliberately does not own.</b> Three rules that govern returns live
/// outside it, and each is outside for a reason:</para>
/// <list type="bullet">
///   <item><description><b>BR-SAL-015</b> (the original invoice must be Committed) — needs the
///   invoice, which is a different aggregate. The handler checks it before constructing this one,
///   so an illegal return is never built at all.</description></item>
///   <item><description><b>BR-SAL-016</b>'s ceiling — derived by summing the lines of other
///   <i>committed</i> returns. Drafts never reserve, which is a deliberate choice with a visible
///   consequence: two drafts can each pass validation and the second fails at commit (the same
///   choice, with the same effect, as BR-PUR-016).</description></item>
///   <item><description><b>BR-SAL-017</b>'s destination and <b>BR-SAL-018</b>'s stock effect —
///   which batches the quantity returns to is read from the recorded consumption trace by
///   <i>Inventory</i>, never by Sales (BR-SAL-013, DEC-SAL-006), and the batch increase, the
///   on-hand increase and the ledger rows happen through the Inventory contract in the same unit of
///   work (BR-INV-062). <see cref="Commit"/> owns only the state transition, exactly as
///   <see cref="SalesInvoice.Commit"/> does.</description></item>
/// </list>
///
/// <para>There is <b>no total</b>: a return has no financial effect whatsoever (DEC-INV-035), and
/// cash refunds remain out of scope (DEC-SAL-001). There is <b>no reason</b>: returns carry none
/// (BR-INV-067). There is <b>no cancellation</b>: a committed return is corrected by an opposing
/// movement (DEC-INV-037).</para>
/// </summary>
public sealed class SalesReturn
{
    private readonly List<SalesReturnLine> _lines = [];

    private SalesReturn()
    {
        // EF Core materialization only.
        Number = string.Empty;
    }

    public SalesReturn(
        Guid id,
        string number,
        Guid salesInvoiceId,
        string? customerName,
        DateOnly returnDate,
        DateTimeOffset createdAt,
        string? notes = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(salesInvoiceId, Guid.Empty);
        // Generated at persist time from the sequence (BR-SAL-014); an absent number here is a
        // programmer error. The SRT- format itself is owned by Infrastructure, like SAL- and PRT-.
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentOutOfRangeException.ThrowIfEqual(returnDate, default);

        Id = id;
        Number = number;
        SalesInvoiceId = salesInvoiceId;
        // Snapshot of the original invoice's customer, which is itself optional free text
        // (BR-SAL-001, DEC-SAL-002) — so blank and absent are the same thing here too.
        CustomerName = string.IsNullOrWhiteSpace(customerName) ? null : customerName.Trim();
        ReturnDate = returnDate;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = createdAt;
        Status = SalesReturnStatus.Draft;
    }

    public Guid Id { get; }

    /// <summary>
    /// System-generated number (BR-SAL-014): the fixed prefix <c>SRT-</c> plus a unique ascending
    /// sequence value (<c>SRT-000001</c>). Assigned once at first persist, never changed, never
    /// reused — and visibly distinct from <c>SAL-</c> so a return is never misread as an invoice.
    /// </summary>
    public string Number { get; private set; }

    /// <summary>The one originating sales invoice (BR-SAL-015, DEC-SAL-010) — never more than one.</summary>
    public Guid SalesInvoiceId { get; private set; }

    /// <summary>Customer-name snapshot from the original invoice; optional, as customers are free text (DEC-SAL-002).</summary>
    public string? CustomerName { get; private set; }

    /// <summary>The return date — required; a business date, no time component.</summary>
    public DateOnly ReturnDate { get; private set; }

    /// <summary>Optional free-text notes.</summary>
    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public SalesReturnStatus Status { get; private set; }

    /// <summary>The return's lines (REQ-SAL-004); a draft may temporarily hold none.</summary>
    public IReadOnlyCollection<SalesReturnLine> Lines => _lines.AsReadOnly();

    /// <summary>
    /// Add a line to a draft return (REQ-SAL-004, BR-SAL-016). The product-name snapshot and the
    /// product's splittability are resolved by the handler from the original sale line and the
    /// catalog. <b>No batch is passed and none is chosen</b> — the destination is derived at commit
    /// from the consumption trace (BR-SAL-017). Only a draft may change (BR-SAL-018); otherwise the
    /// call is rejected without mutation. There is no edit path: correcting a line is remove then
    /// add, the BR-SAL-004 precedent.
    /// </summary>
    public SalesReturnLine AddLine(
        Guid lineId,
        Guid salesLineItemId,
        Guid productId,
        string productName,
        decimal quantity,
        bool allowsFractionalQuantity,
        DateTimeOffset addedAt)
    {
        EnsureDraft();

        var line = new SalesReturnLine(
            lineId,
            salesLineItemId,
            productId,
            productName,
            quantity,
            allowsFractionalQuantity,
            addedAt);

        _lines.Add(line);
        return line;
    }

    /// <summary>
    /// Remove a line from a draft return by id (BR-SAL-018). Returns <c>false</c> when the line is
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
    /// Commit a draft return (BR-SAL-018): the one-time, all-or-nothing transition Draft →
    /// Committed. Only a draft with at least one line may be committed; committing a non-draft or
    /// an empty return is rejected without mutation.
    ///
    /// <para>The inventory effect — the batch increases, the on-hand increase and one ledger row
    /// per batch — is applied by the commit handler through the Inventory contract in the same unit
    /// of work (BR-INV-062); this method owns only the state transition. Afterwards the document is
    /// immutable (BR-SAL-018): the Draft-only guards block every further change and a second commit
    /// is rejected here.</para>
    /// </summary>
    public void Commit()
    {
        EnsureDraft();

        if (_lines.Count == 0)
        {
            throw new BusinessRuleException(SalesErrorCodes.ReturnHasNoLines);
        }

        Status = SalesReturnStatus.Committed;
    }

    private void EnsureDraft()
    {
        if (Status != SalesReturnStatus.Draft)
        {
            throw new BusinessRuleException(SalesErrorCodes.ReturnNotDraft);
        }
    }
}
