using System.Globalization;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// The sales-invoice number format (BR-SAL-002): the fixed prefix <c>SAL-</c> followed by a unique
/// ascending sequence value, zero-padded to at least six digits (<c>SAL-000001</c>), growing past
/// six digits automatically. Generation is a system responsibility owned here; the PostgreSQL
/// sequence guarantees uniqueness under concurrency, and gaps are acceptable. A literal copy of the
/// twice-implemented pattern (BR-PUR-002 / <see cref="Purchasing.InternalPurchaseInvoiceNumber"/>,
/// DEC-CAT-026) — not an invention.
/// </summary>
public static class InternalSalesInvoiceNumber
{
    public const string Prefix = "SAL-";
    public const int MinDigits = 6;

    /// <summary>The PostgreSQL sequence backing the ascending number (created by migration).</summary>
    public const string SequenceName = "sales_invoice_number_seq";

    public static string Format(long sequenceValue) =>
        Prefix + sequenceValue.ToString("D" + MinDigits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
