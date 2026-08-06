using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Queries.PurchasingDashboardSummary;
using VetFlow.Infrastructure.Organization;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The dashboard's <b>failure</b> path (DEC-DSH-002, BR-DSH-014) — the one part of the module
/// that no amount of healthy data will ever execute.
/// <para>
/// Every other test here runs against a working stack, so all seven sections come back
/// <c>ok</c> and the failed-section branch is never reached. That is precisely the shape of
/// defect this repository keeps finding: the nineteenth cycle's `401` on the application shell
/// survived a full browser pass because the code path <i>only runs when a published bundle is
/// present</i>, and the verification ran against the dev server. So this test forces the
/// failure rather than waiting for one.
/// </para>
/// <para>
/// It builds its own host — the shared fixture deliberately has no per-test service seam — but
/// reuses the same database, so everything except the one broken read is real.
/// </para>
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class DashboardSectionFailureTests(ApiFixture fixture) : IAsyncLifetime
{
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Database:ConnectionString", fixture.ConnectionString);
            builder.UseSetting("Database:ApplyMigrationsAtStartup", "false");
            builder.UseSetting("Serilog:MinimumLevel:Default", "Error");
            builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-chars");

            builder.ConfigureServices(services =>
                services.Replace(ServiceDescriptor.Scoped<
                    IQueryHandler<PurchasingDashboardSummaryQuery, PurchasingDashboardSummaryDto>,
                    ThrowingPurchasingSummaryHandler>()));
        });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await ApiFixture.SignInAsync(
                _client,
                OrganizationSeeder.PilotOwnerPhoneNumber,
                OrganizationSeeder.PilotOwnerPhoneNumber));
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task One_broken_module_does_not_fail_the_board_DEC_DSH_002()
    {
        var sections = await GetSectionsAsync();

        // The request still succeeds — a dashboard that 500s because one module is slow would
        // blank six healthy sections to report one broken one.
        sections.GetProperty("draftPurchases").GetProperty("status").GetString().ShouldBe("failed");

        foreach (var healthy in new[]
                 {
                     "expiredBatches", "outOfStockProducts", "expiringSoonBatches",
                     "draftSales", "todaySales", "recentMovements",
                 })
        {
            sections.GetProperty(healthy).GetProperty("status").GetString()
                .ShouldBe("ok", $"section '{healthy}' should be unaffected");
        }
    }

    [Fact]
    public async Task A_failed_section_carries_no_count_and_is_never_zero_BR_DSH_014()
    {
        var failed = (await GetSectionsAsync()).GetProperty("draftPurchases");

        // ⛔ The whole point: «no draft purchases» and «could not determine draft purchases»
        // are contradictory statements. Serialising a 0 here would turn an outage into false
        // reassurance — and inside the expiry sections that is a safety decision (DEC-INV-021).
        failed.TryGetProperty("count", out _).ShouldBeFalse("a failed section must omit its count, not zero it");
    }

    [Fact]
    public async Task The_section_key_is_still_present_so_absent_and_zero_cannot_be_confused_BR_DSH_014()
    {
        // Dropping the key entirely would leave the client unable to tell «failed» from
        // «this section no longer exists» — so the key stays and only the data goes.
        (await GetSectionsAsync()).TryGetProperty("draftPurchases", out _).ShouldBeTrue();
    }

    private async Task<JsonElement> GetSectionsAsync()
    {
        var client = _client ?? throw new InvalidOperationException("Fixture not initialized.");
        var response = await client.GetAsync(new Uri("/api/v1/dashboard", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("sections").Clone();
    }

    /// <summary>Stands in for an owning module that cannot answer right now.</summary>
    private sealed class ThrowingPurchasingSummaryHandler
        : IQueryHandler<PurchasingDashboardSummaryQuery, PurchasingDashboardSummaryDto>
    {
        public Task<PurchasingDashboardSummaryDto> HandleAsync(
            PurchasingDashboardSummaryQuery query,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Purchasing is unavailable.");
    }
}
