using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Common.Behaviors;

/// <summary>
/// Pipeline decorator (ADR-0014 §6, STD-BE-029): runs the command's
/// FluentValidation validators and rejects invalid input with the domain
/// <see cref="Domain.Common.ValidationException"/> — the per-field
/// <c>errors</c> shape (STD-API-014) — before the handler runs. Mirrors the
/// read-side <see cref="ValidatingQueryHandler{TQuery,TResult}"/>.
/// </summary>
public sealed class ValidatingCommandHandler<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(command, cancellationToken);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            var fieldErrors = failures
                .GroupBy(failure => failure.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());
            throw new Domain.Common.ValidationException(fieldErrors);
        }

        return await inner.HandleAsync(command, cancellationToken);
    }
}
