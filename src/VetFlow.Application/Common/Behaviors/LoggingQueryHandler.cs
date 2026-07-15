using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace VetFlow.Application.Common.Behaviors;

/// <summary>
/// Pipeline decorator (ADR-0014 §6): structured timing log per handled query.
/// Logs the query type and duration only — never payload content (STD-BE-050).
/// </summary>
public sealed partial class LoggingQueryHandler<TQuery, TResult>(
    IQueryHandler<TQuery, TResult> inner,
    ILogger<LoggingQueryHandler<TQuery, TResult>> logger) : IQueryHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    public async Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await inner.HandleAsync(query, cancellationToken);
            LogHandled(logger, typeof(TQuery).Name, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception exception)
        {
            LogFailed(logger, exception, typeof(TQuery).Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {QueryType} in {ElapsedMs} ms")]
    private static partial void LogHandled(ILogger logger, string queryType, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Query {QueryType} failed after {ElapsedMs} ms")]
    private static partial void LogFailed(ILogger logger, Exception exception, string queryType, long elapsedMs);
}
