using System.Net;
using System.Text.Json;
using Shouldly;

namespace VetFlow.IntegrationTests;

/// <summary>Every non-2xx is RFC 9457 problem+json (STD-API-010, STD-API-013, STD-API-014).</summary>
[Collection(ApiCollection.Name)]
public sealed class ProblemDetailsTests(ApiFixture fixture)
{
    private const string ProblemContentType = "application/problem+json";

    [Fact]
    public async Task Invalid_page_returns_the_canonical_validation_shape_STD_API_014()
    {
        var response = await fixture.Client.GetAsync(new Uri("/api/v1/products?page=0", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemContentType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errorCode").GetString().ShouldBe("VTF-VAL-001");
        body.RootElement.GetProperty("type").GetString().ShouldBe("https://vetflow.app/errors/VTF-VAL-001");
        body.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
        var pageErrors = body.RootElement.GetProperty("errors").GetProperty("page");
        pageErrors.GetArrayLength().ShouldBe(1);
        pageErrors[0].GetString().ShouldBe("رقم الصفحة يجب ألا يقل عن 1.");
    }

    [Fact]
    public async Task An_overflowing_page_number_is_a_clean_validation_error_not_a_500_STD_BE_027()
    {
        // Regression (R2): a huge page once overflowed the Int32 offset
        // (Page - 1) * PageSize and surfaced as an unhandled 500. It must now be a
        // bounded 400 validation error on both the product list and the lookups.
        var product = await fixture.Client.GetAsync(
            new Uri("/api/v1/products?page=2147483647", UriKind.Relative));
        var lookup = await fixture.Client.GetAsync(
            new Uri("/api/v1/manufacturers?page=2147483647", UriKind.Relative));

        foreach (var response in new[] { product, lookup })
        {
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemContentType);
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            body.RootElement.GetProperty("errorCode").GetString().ShouldBe("VTF-VAL-001");
            body.RootElement.GetProperty("errors").GetProperty("page")
                .EnumerateArray().Select(error => error.GetString())
                .ShouldContain("رقم الصفحة أكبر من الحد المسموح.");
        }
    }

    [Fact]
    public async Task Malformed_ids_and_unknown_sort_produce_field_errors_STD_API_014()
    {
        var response = await fixture.Client.GetAsync(
            new Uri("/api/v1/products?categoryId=not-a-guid&sort=weird", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        errors.TryGetProperty("categoryId", out _).ShouldBeTrue();
        errors.TryGetProperty("sort", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Validation_messages_localize_to_english_on_accept_language_ADR_0007()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/products?page=0", UriKind.Relative));
        request.Headers.AcceptLanguage.ParseAdd("en");

        var response = await fixture.Client.SendAsync(request);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").GetProperty("page")[0].GetString()
            .ShouldBe("Page must be at least 1.");
    }

    [Fact]
    public async Task Unknown_routes_return_problem_details_404_STD_API_010()
    {
        var response = await fixture.Client.GetAsync(new Uri("/api/v1/does-not-exist", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemContentType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(404);
        body.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Every_response_carries_a_correlation_id_STD_API_033()
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/products?categoryId={Guid.NewGuid()}", UriKind.Relative));

        response.Headers.TryGetValues("X-Correlation-ID", out var values).ShouldBeTrue();
        values.ShouldHaveSingleItem().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task An_incoming_correlation_id_is_echoed_back_STD_API_033()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, new Uri($"/api/v1/products?categoryId={Guid.NewGuid()}", UriKind.Relative));
        request.Headers.Add("X-Correlation-ID", "caller-supplied-id");

        var response = await fixture.Client.SendAsync(request);

        response.Headers.GetValues("X-Correlation-ID").ShouldHaveSingleItem().ShouldBe("caller-supplied-id");
    }
}
