namespace VetFlow.Application.Common;

/// <summary>
/// Marker for a write-side request (CQRS-lite, ADR-0014 §5). A command mutates
/// state through exactly one aggregate and one transaction (STD-BE-024). The
/// type parameter binds the command to its lightweight result — an id or a
/// command result, never a read DTO (STD-BE-021).
/// </summary>
/// <typeparam name="TResult">The lightweight result the command produces.</typeparam>
public interface ICommand<TResult>;
