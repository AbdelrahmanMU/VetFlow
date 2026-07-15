namespace VetFlow.Api;

/// <summary>Validated typed CORS options (STD-BE-048).</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";
    public const string PolicyName = "VetFlowWeb";

    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];
}
