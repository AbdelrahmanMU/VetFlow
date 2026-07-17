using VetFlow.Application.Common;

namespace VetFlow.Application.Purchasing.Commands.CreatePurchaseInvoice;

/// <summary>
/// Create a purchase invoice (REQ-PUR-003, WF creation slice): the header only —
/// the required supplier name, the optional supplier reference, the required
/// invoice date, and optional notes (BR-PUR-001). The system number (BR-PUR-002)
/// is generated at persist time and is never supplied here; the invoice is always
/// born a <see cref="Domain.Purchasing.PurchaseInvoiceStatus.Draft"/> (BR-PUR-003)
/// with a zero total — line items and total derivation belong to a later slice
/// (DEC-PUR-001, scope lock).
/// </summary>
public sealed record CreatePurchaseInvoiceCommand : ICommand<CreatePurchaseInvoiceResult>
{
    public required string SupplierName { get; init; }

    public string? SupplierInvoiceReference { get; init; }

    /// <summary>Required; validated <c>NotNull</c> so a missing date is a per-field 400 (AC-PUR-007).</summary>
    public DateOnly? InvoiceDate { get; init; }

    public string? Notes { get; init; }
}
