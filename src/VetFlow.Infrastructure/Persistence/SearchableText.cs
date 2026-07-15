namespace VetFlow.Infrastructure.Persistence;

/// <summary>The shadow-property name of normalized search text columns (STD-BE-044).</summary>
public static class SearchableText
{
    public const string PropertyName = "SearchText";
    public const int MaxLength = 700;
}
