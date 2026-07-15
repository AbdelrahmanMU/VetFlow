namespace VetFlow.Domain.Common;

/// <summary>
/// Root of all business-meaningful failures (ADR-0018). Carries a stable error
/// code and optional structured metadata — never user-facing text, HTTP status,
/// or UI copy. Translation happens exclusively in the API middleware.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string errorCode, IReadOnlyDictionary<string, string>? metadata)
        : base(errorCode)
    {
        ErrorCode = errorCode;
        Metadata = metadata ?? new Dictionary<string, string>();
    }

    public string ErrorCode { get; }

    public IReadOnlyDictionary<string, string> Metadata { get; }
}
