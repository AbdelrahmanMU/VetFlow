namespace VetFlow.Infrastructure.Persistence;

/// <summary>
/// Validated typed database options (STD-BE-048): invalid configuration
/// refuses to boot (principle 8).
/// </summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public required string ConnectionString { get; init; }

    /// <summary>Development convenience only; production applies reviewed migrations explicitly.</summary>
    public bool ApplyMigrationsAtStartup { get; init; }

    /// <summary>
    /// Seeds a handful of sample purchase invoices at startup for local browser
    /// verification (DEC-PUR-001). Development only, off by default, and idempotent
    /// (it seeds only when the table is empty) — production and tests never enable it.
    /// </summary>
    public bool SeedDevelopmentDataAtStartup { get; init; }
}
