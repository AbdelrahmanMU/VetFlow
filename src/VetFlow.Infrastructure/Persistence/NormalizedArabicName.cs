namespace VetFlow.Infrastructure.Persistence;

/// <summary>
/// The shadow-property name of a product's normalized Arabic-name column. It
/// backs the possible-duplicate advisory (DEC-CAT-027), which matches on the
/// Arabic name specifically — kept separate from <see cref="SearchableText"/>,
/// whose combined Arabic+English text would dilute the similarity score.
/// </summary>
public static class NormalizedArabicName
{
    public const string PropertyName = "ArabicNameNormalized";
    public const int MaxLength = 300;
}
