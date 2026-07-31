using System.Globalization;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// The sales-return number format (BR-SAL-014, owner ruling DEC-SAL-010): the fixed prefix
/// <c>SRT-</c> followed by a unique ascending sequence value, zero-padded to at least six digits
/// (<c>SRT-000001</c>) and growing past six digits automatically.
///
/// <para>This is the <b>same mechanism</b> as <see cref="InternalSalesInvoiceNumber"/> and
/// <c>InternalPurchaseReturnNumber</c> — a PostgreSQL sequence read with <c>nextval</c> at create
/// time — not a second numbering scheme. The prefix is the only thing that differs, and it differs
/// on purpose: <c>SRT-</c> is visibly distinct from <c>SAL-</c> so a return can never be misread as
/// an invoice.</para>
/// </summary>
public static class InternalSalesReturnNumber
{
    public const string Prefix = "SRT-";
    public const int MinDigits = 6;

    /// <summary>The PostgreSQL sequence backing the ascending number (created by migration).</summary>
    public const string SequenceName = "sales_return_number_seq";

    public static string Format(long sequenceValue) =>
        Prefix + sequenceValue.ToString("D" + MinDigits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
