using System.Text;

namespace VetFlow.Infrastructure.Persistence;

/// <summary>
/// Write-time Arabic search normalization (ADR-0019, STD-BE-044): diacritics
/// stripped, alef/teh-marbuta/alef-maqsura forms unified, Latin lower-cased,
/// whitespace collapsed. The same normalization is applied to search input so
/// stored text and query text always meet in the same form.
/// </summary>
public static class ArabicSearchText
{
    private const char ArabicTatweel = 'ـ';

    public static string Normalize(string primary, string? secondary = null)
    {
        var builder = new StringBuilder(primary.Length + (secondary?.Length ?? 0) + 1);
        AppendNormalized(builder, primary);
        if (!string.IsNullOrWhiteSpace(secondary))
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            AppendNormalized(builder, secondary);
        }

        return builder.ToString();
    }

    private static void AppendNormalized(StringBuilder builder, string text)
    {
        var pendingSpace = false;
        foreach (var character in text)
        {
            if (IsRemovable(character))
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(Unify(character));
        }
    }

    private static bool IsRemovable(char character) =>
        character is >= 'ً' and <= 'ٟ' || character is 'ٰ' || character is ArabicTatweel;

    private static char Unify(char character) => character switch
    {
        'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
        'ة' => 'ه',
        'ى' => 'ي',
        'ؤ' => 'و',
        'ئ' => 'ي',
        _ => char.ToLowerInvariant(character),
    };
}
