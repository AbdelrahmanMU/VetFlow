namespace VetFlow.Domain.Common;

/// <summary>
/// A documented business rule (BR-*) was violated. The error code identifies
/// exactly one business rule in the Error Catalog (ADR-0018).
/// </summary>
public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string errorCode, IReadOnlyDictionary<string, string>? metadata = null)
        : base(errorCode, metadata)
    {
    }
}
