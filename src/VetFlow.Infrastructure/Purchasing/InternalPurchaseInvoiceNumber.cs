using System.Globalization;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// The purchase-invoice number format (BR-PUR-002): the fixed prefix <c>PUR-</c>
/// followed by a unique ascending value, zero-padded to at least six
/// digits (<c>PUR-000001</c>), growing past six digits automatically. Generation
/// is a system responsibility owned here (mirrors the Catalog internal-code pattern,
/// DEC-CAT-026 / <see cref="Catalog.InternalProductCode"/>).
///
/// <para><b>The format is unchanged</b>; the source is now a counter per branch rather
/// than a database-global sequence (ADR-0022 §6), so every branch's accounting series
/// starts at one and no failed save burns a number.</para>
/// </summary>
public static class InternalPurchaseInvoiceNumber
{
    public const string Prefix = "PUR-";
    public const int MinDigits = 6;

    public static string Format(long sequenceValue) =>
        Prefix + sequenceValue.ToString("D" + MinDigits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
