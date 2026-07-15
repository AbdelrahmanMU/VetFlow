using FluentValidation;
using VetFlow.Application.Common;

namespace VetFlow.Application.Common.Behaviors;

/// <summary>
/// Pipeline decorator (ADR-0014 §6): runs the query's FluentValidation
/// validators and rejects invalid input with the domain
/// <see cref="Domain.Common.ValidationException"/> before the handler runs.
/// </summary>
public sealed class ValidatingQueryHandler<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    IEnumerable<IValidator<TQuery>> validators) : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(query, cancellationToken);
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

        return await inner.HandleAsync(query, cancellationToken);
    }
}
