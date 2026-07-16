namespace VetFlow.Domain.Purchasing;

/// <summary>
/// Purchase-invoice lifecycle status (BR-PUR-003): a draft becomes received (the
/// receiving slice) or is cancelled while still a draft; received and cancelled
/// are terminal. Slice 1 constructs invoices only as <see cref="Draft"/> — the
/// transitions themselves ship with their own slices.
/// </summary>
public enum PurchaseInvoiceStatus
{
    Draft = 1,
    Received = 2,
    Cancelled = 3,
}
