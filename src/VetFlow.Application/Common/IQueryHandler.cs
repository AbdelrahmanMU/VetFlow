namespace VetFlow.Application.Common;

/// <summary>
/// The single read-side use-case abstraction (ADR-0014 §6 — the project owns
/// its pipeline; no mediator). Query contracts live in Application; their
/// implementations live in Infrastructure (ADR-0014 §5, STD-BE-023).
/// </summary>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
