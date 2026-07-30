using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Localization;
using VetFlow.Api.Errors;

namespace VetFlow.Api.Middleware;

/// <summary>
/// Writes RFC 9457 <c>application/problem+json</c> documents — the single
/// response shape of every non-2xx (STD-API-010), always carrying the traceId
/// (STD-API-033).
/// </summary>
public static class ProblemDetailsWriter
{
    public const string ContentType = "application/problem+json";
    private const string ErrorTypeBaseUri = "https://vetflow.app/errors/";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(
        HttpContext context,
        int status,
        string title,
        string? detail,
        string? errorCode,
        IReadOnlyDictionary<string, string[]>? errors = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        var problem = new Dictionary<string, object?>
        {
            ["type"] = errorCode is null ? "about:blank" : ErrorTypeBaseUri + errorCode,
            ["title"] = title,
            ["status"] = status,
            ["instance"] = context.Request.Path.Value,
            ["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier,
        };

        if (detail is not null)
        {
            problem["detail"] = detail;
        }

        if (errorCode is not null)
        {
            problem["errorCode"] = errorCode;
        }

        if (errors is not null)
        {
            problem["errors"] = errors;
        }

        // Structured, already-non-translatable facts the client needs to compose a precise message
        // — the products a sale could not cover (AC-INV-041, AC-SAL-009), the line whose conversion
        // was inexact (AC-SAL-013). Data, never UI copy: the wording stays with the localized
        // detail above (ADR-0018).
        if (metadata is { Count: > 0 })
        {
            problem["metadata"] = metadata;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = ContentType;
        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, SerializerOptions));
    }

    public static CultureInfo RequestCulture(HttpContext context) =>
        context.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture
        ?? CultureInfo.CurrentUICulture;

    public static string Localize(HttpContext context, string key) =>
        ErrorMessages.Get(key, RequestCulture(context));
}
