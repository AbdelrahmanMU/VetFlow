using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using VetFlow.Api.Middleware;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Common;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The single translation point (ADR-0018, STD-BE-063): business rule error
/// codes map to their catalog HTTP status, and unexpected failures stay opaque.
/// No endpoint in this slice raises these codes yet, so the middleware is
/// exercised directly.
/// </summary>
public sealed class ExceptionTranslationTests
{
    [Theory]
    [InlineData(CatalogErrorCodes.MinimumProductData)]
    [InlineData(CatalogErrorCodes.UnitProfileComposition)]
    [InlineData(CatalogErrorCodes.StorageUnitNotInProfile)]
    [InlineData(CatalogErrorCodes.DefaultPurchaseUnitInvalid)]
    [InlineData(CatalogErrorCodes.DefaultSaleUnitInvalid)]
    [InlineData(CatalogErrorCodes.PriceOnNonSaleUnit)]
    [InlineData(CatalogErrorCodes.OpenExpirationPeriodRequired)]
    public async Task Business_rule_codes_translate_to_their_catalog_status_STD_BE_063(string errorCode)
    {
        var (status, body) = await InvokeAsync(new BusinessRuleException(errorCode));

        status.ShouldBe(StatusCodes.Status400BadRequest);
        body.RootElement.GetProperty("errorCode").GetString().ShouldBe(errorCode);
        body.RootElement.GetProperty("type").GetString().ShouldBe($"https://vetflow.app/errors/{errorCode}");
        body.RootElement.GetProperty("detail").GetString().ShouldNotBe(errorCode);
    }

    [Fact]
    public async Task Unexpected_failures_are_an_opaque_500_with_a_trace_id_STD_API_013()
    {
        var (status, body) = await InvokeAsync(new InvalidOperationException("secret internals"));

        status.ShouldBe(StatusCodes.Status500InternalServerError);
        body.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        body.RootElement.TryGetProperty("errorCode", out _).ShouldBeFalse();
        var raw = body.RootElement.GetRawText();
        raw.ShouldNotContain("secret internals");
        raw.ShouldNotContain("InvalidOperationException");
    }

    private static async Task<(int Status, JsonDocument Body)> InvokeAsync(Exception exception)
    {
        var middleware = new ExceptionTranslationMiddleware(
            _ => throw exception,
            NullLogger<ExceptionTranslationMiddleware>.Instance);

        var context = new DefaultHttpContext();
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await middleware.InvokeAsync(context);

        responseBody.Position = 0;
        using var reader = new StreamReader(responseBody);
        var body = JsonDocument.Parse(await reader.ReadToEndAsync());
        return (context.Response.StatusCode, body);
    }
}
