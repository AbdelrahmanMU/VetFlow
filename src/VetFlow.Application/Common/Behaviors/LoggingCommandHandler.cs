using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace VetFlow.Application.Common.Behaviors;

/// <summary>
/// Pipeline decorator (ADR-0014 §6, STD-BE-029): structured timing log per
/// handled command. Logs the command type and duration only — never payload
/// content (STD-BE-050). Mirrors the read-side
/// <see cref="LoggingQueryHandler{TQuery,TResult}"/>.
/// </summary>
public sealed partial class LoggingCommandHandler<TCommand, TResult>(
    ICommandHandler<TCommand, TResult> inner,
    ILogger<LoggingCommandHandler<TCommand, TResult>> logger) : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    public async Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await inner.HandleAsync(command, cancellationToken);
            LogHandled(logger, typeof(TCommand).Name, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception exception)
        {
            LogFailed(logger, exception, typeof(TCommand).Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {CommandType} in {ElapsedMs} ms")]
    private static partial void LogHandled(ILogger logger, string commandType, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Command {CommandType} failed after {ElapsedMs} ms")]
    private static partial void LogFailed(ILogger logger, Exception exception, string commandType, long elapsedMs);
}
