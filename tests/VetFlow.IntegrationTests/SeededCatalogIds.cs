namespace VetFlow.IntegrationTests;

/// <summary>
/// Fixed identifiers of the migration-seeded units (REQ-CAT-016) and natures
/// (AC-CAT-011); must match the HasData seeds in Infrastructure.
/// </summary>
public static class SeededCatalogIds
{
    public static readonly Guid PieceUnit = Guid.Parse("a1000000-0000-4000-8000-000000000001");
    public static readonly Guid BoxUnit = Guid.Parse("a1000000-0000-4000-8000-000000000002");
    public static readonly Guid CartonUnit = Guid.Parse("a1000000-0000-4000-8000-000000000003");
    public static readonly Guid StripUnit = Guid.Parse("a1000000-0000-4000-8000-000000000004");
    public static readonly Guid PillUnit = Guid.Parse("a1000000-0000-4000-8000-000000000005");
    public static readonly Guid BottleUnit = Guid.Parse("a1000000-0000-4000-8000-000000000006");
    public static readonly Guid CentimeterUnit = Guid.Parse("a1000000-0000-4000-8000-000000000007");

    public static readonly Guid MedicineNature = Guid.Parse("b1000000-0000-4000-8000-000000000001");
    public static readonly Guid FoodNature = Guid.Parse("b1000000-0000-4000-8000-000000000002");
    public static readonly Guid MedicalSupplyNature = Guid.Parse("b1000000-0000-4000-8000-000000000003");

    public static readonly IReadOnlyList<string> DefaultUnitNames =
    [
        "قطعة", "علبة", "كرتونة", "شريط", "قرص", "زجاجة", "سم",
        "مل", "لتر", "جرام", "كيلوجرام", "شيكارة", "متر",
    ];

    public static readonly IReadOnlyList<string> InitialNatureNames =
    [
        "دواء", "غذاء", "مستلزم طبي", "مستلزم حيوانات", "منتج عناية",
    ];
}
