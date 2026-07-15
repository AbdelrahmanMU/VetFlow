namespace VetFlow.Domain.Common;

/// <summary>Cross-module error codes (ADR-0018, ADR-0015 §4).</summary>
public static class CommonErrorCodes
{
    /// <summary>Input failed validation; the canonical validation Problem Details shape.</summary>
    public const string Validation = "VTF-VAL-001";
}
