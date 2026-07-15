using System.Text.Json;
using VetFlow.Api.Errors;
using VetFlow.Domain.Common;

namespace VetFlow.Api.Middleware;

/// <summary>
/// The single translation point (ADR-0018): domain exceptions become RFC 9457
/// Problem Details with the stable error code, the mapped HTTP status, and the
/// localized message. Unexpected failures become an opaque 500 with a traceId —
/// never a stack trace (STD-API-013).
/// </summary>
public sealed partial class ExceptionTranslationMiddleware(
    RequestDelegate next,
    ILogger<ExceptionTranslationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            var entry = ErrorCatalog.Get(exception.ErrorCode);
            var localizedErrors = exception.FieldErrors.ToDictionary(
                fieldError => JsonNamingPolicy.CamelCase.ConvertName(fieldError.Key),
                fieldError => fieldError.Value
                    .Select(messageKey => ProblemDetailsWriter.Localize(context, messageKey))
                    .ToArray());

            await ProblemDetailsWriter.WriteAsync(
                context,
                entry.Status,
                entry.Title,
                ProblemDetailsWriter.Localize(context, exception.ErrorCode),
                exception.ErrorCode,
                localizedErrors);
        }
        catch (DomainException exception)
        {
            var entry = ErrorCatalog.Get(exception.ErrorCode);
            LogBusinessFailure(logger, exception.ErrorCode, context.Request.Path);

            await ProblemDetailsWriter.WriteAsync(
                context,
                entry.Status,
                entry.Title,
                ProblemDetailsWriter.Localize(context, exception.ErrorCode),
                exception.ErrorCode);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogUnexpectedFailure(logger, exception, context.Request.Path);

            await ProblemDetailsWriter.WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                ProblemDetailsWriter.Localize(context, "error.internal"),
                errorCode: null);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Business failure {ErrorCode} on {Path}")]
    private static partial void LogBusinessFailure(ILogger logger, string errorCode, string path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception on {Path}")]
    private static partial void LogUnexpectedFailure(ILogger logger, Exception exception, string path);
}
