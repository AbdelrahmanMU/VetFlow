using System.Globalization;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// The internal-code format (DEC-CAT-026, BR-CAT-006): the fixed prefix
/// <c>PRD-</c> followed by a unique ascending value, zero-padded to at
/// least six digits (<c>PRD-000001</c>), growing past six digits automatically
/// beyond a million. Generation is a system responsibility owned here.
///
/// <para><b>The format is exactly what it always was</b>; only where the number comes
/// from changed. It used to be a database-global sequence, and is now a counter
/// per tenant (ADR-0022 §6) — so a second clinic's first product is
/// <c>PRD-000001</c> and not the continuation of another clinic's numbering.
/// The catalog is tenant-wide, not branch-wide (DEC-ORG-006).</para>
/// </summary>
public static class InternalProductCode
{
    public const string Prefix = "PRD-";
    public const int MinDigits = 6;

    public static string Format(long sequenceValue) =>
        Prefix + sequenceValue.ToString("D" + MinDigits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
