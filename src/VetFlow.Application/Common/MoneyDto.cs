namespace VetFlow.Application.Common;

/// <summary>Money is always a decimal amount plus a currency code (STD-API-031, ADR-0007).</summary>
public sealed record MoneyDto
{
    public required decimal Amount { get; init; }

    public required string Currency { get; init; }
}
