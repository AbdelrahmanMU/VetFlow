namespace VetFlow.Domain.Common;

/// <summary>
/// Input failed validation before reaching a handler (ADR-0018). Field errors
/// carry message resource keys, not user-facing text — the API middleware is
/// the single translation point (STD-API-012).
/// </summary>
public sealed class ValidationException : DomainException
{
    public ValidationException(IReadOnlyDictionary<string, string[]> fieldErrors)
        : base(CommonErrorCodes.Validation, null)
    {
        FieldErrors = fieldErrors;
    }

    /// <summary>Field name → message resource keys.</summary>
    public IReadOnlyDictionary<string, string[]> FieldErrors { get; }
}
