using System.Globalization;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// The sales-invoice number format (BR-SAL-002): the fixed prefix <c>SAL-</c> followed by a unique
/// ascending value, zero-padded to at least six digits (<c>SAL-000001</c>), growing past
/// six digits automatically. Generation is a system responsibility owned here. A literal copy of the
/// twice-implemented pattern (BR-PUR-002 / <see cref="Purchasing.InternalPurchaseInvoiceNumber"/>,
/// DEC-CAT-026) — not an invention.
///
/// <para><b>The format is unchanged</b>; the source is a counter per branch (ADR-0022 §6).
/// <b>Gaps are no longer acceptable</b> — the owner ruled numbering gapless on 2026-08-02, which is
/// achievable because the number is now allocated inside the transaction that inserts the invoice
/// and rolls back with it.</para>
/// </summary>
public static class InternalSalesInvoiceNumber
{
    public const string Prefix = "SAL-";
    public const int MinDigits = 6;

    public static string Format(long sequenceValue) =>
        Prefix + sequenceValue.ToString("D" + MinDigits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
