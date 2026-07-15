namespace VetFlow.Application.Common;

/// <summary>
/// The single write-side use-case abstraction (STD-BE-020 — the project owns its
/// pipeline; no mediator). Command contracts live in Application; their
/// implementations live in Infrastructure (ADR-0014 §5, STD-BE-023), where the
/// one command = one <c>SaveChanges</c> boundary is owned (STD-BE-024).
/// </summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
