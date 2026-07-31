using System.Globalization;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// The purchase-return number format (BR-PUR-014, owner ruling DEC-PUR-010): the fixed prefix
/// <c>PRT-</c> followed by a unique ascending sequence value, zero-padded to at least six digits
/// (<c>PRT-000001</c>) and growing past six digits automatically.
///
/// <para>This is the <b>same mechanism</b> as <see cref="InternalPurchaseInvoiceNumber"/> and the
/// catalog's product code — a PostgreSQL sequence read with <c>nextval</c> at create time — not a
/// second numbering scheme. The prefix is the only thing that differs, and it differs on purpose:
/// <c>PRT-</c> is visibly distinct from <c>PUR-</c> so a return can never be misread as an
/// invoice.</para>
/// </summary>
public static class InternalPurchaseReturnNumber
{
    public const string Prefix = "PRT-";
    public const int MinDigits = 6;

    /// <summary>The PostgreSQL sequence backing the ascending number (created by migration).</summary>
    public const string SequenceName = "purchase_return_number_seq";

    public static string Format(long sequenceValue) =>
        Prefix + sequenceValue.ToString("D" + MinDigits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
