using System.Globalization;

namespace VetFlow.Infrastructure.Catalog;

/// <summary>
/// The internal-code format (DEC-CAT-026, BR-CAT-006): the fixed prefix
/// <c>PRD-</c> followed by a unique ascending sequence value, zero-padded to at
/// least six digits (<c>PRD-000001</c>), growing past six digits automatically
/// beyond a million. Generation is a system responsibility owned here; the
/// sequence guarantees uniqueness under concurrency.
/// </summary>
public static class InternalProductCode
{
    public const string Prefix = "PRD-";
    public const int MinDigits = 6;

    /// <summary>The PostgreSQL sequence backing the ascending code (created by migration).</summary>
    public const string SequenceName = "product_internal_code_seq";

    public static string Format(long sequenceValue) =>
        Prefix + sequenceValue.ToString("D" + MinDigits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
}
