using System.Globalization;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// The purchase-invoice number format (BR-PUR-002): the fixed prefix <c>PUR-</c>
/// followed by a unique ascending sequence value, zero-padded to at least six
/// digits (<c>PUR-000001</c>), growing past six digits automatically. Generation
/// is a system responsibility owned here; the PostgreSQL sequence guarantees
/// uniqueness under concurrency (mirrors the Catalog internal-code pattern,
/// DEC-CAT-026 / <see cref="Catalog.InternalProductCode"/>).
/// </summary>
public static class InternalPurchaseInvoiceNumber
{
    public const string Prefix = "PUR-";
    public const int MinDigits = 6;

    /// <summary>The PostgreSQL sequence backing the ascending number (created by migration).</summary>
    public const string SequenceName = "purchase_invoice_number_seq";

    public static string Format(long sequenceValue) =>
        Prefix + sequenceValue.ToString("D" + MinDigits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
