namespace VetFlow.Application.Common;

/// <summary>
/// Marker for a read-side request (CQRS-lite, ADR-0014 §5). A query never
/// mutates state and never triggers business side effects. The type parameter
/// binds the query to its result type for pipeline resolution.
/// </summary>
/// <typeparam name="TResult">The result the query produces.</typeparam>
public interface IQuery<TResult>;
