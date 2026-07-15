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
}
