namespace VetFlow.Application.Common;

/// <summary>A selectable option of a managed lookup (category, manufacturer, product nature).</summary>
public sealed record LookupOptionDto
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }
}
