using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using VetFlow.Infrastructure.Organization;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The Pilot's same-origin hosting: the API serves the published Angular bundle from
/// <c>wwwroot</c> (ADR-0021, PRS WS1). <b>This path exists only when that bundle is present</b>,
/// so it is dormant in development — `ng serve` proxies instead — and every other test in this
/// suite runs with it switched off.
///
/// <b>That is exactly why it needs its own test.</b> When every endpoint began demanding a token,
/// the shell itself started returning 401: the authorization middleware's fallback policy applies
/// to requests with no endpoint too, and static files were being served after it. The login screen
/// could not load, so nobody could sign in to discover why. It reached a deployment because the
/// browser verification ran against the dev server, where this code never executes.
/// </summary>
public sealed class SpaHostingTests : IAsyncLifetime
{
    private const string IndexMarkup = "<!doctype html><html lang=\"ar\" dir=\"rtl\"><head><title>VetFlow</title></head><body>vetflow-shell</body></html>";

    private readonly ApiFixture _database = new();
    private string _webRoot = string.Empty;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        // The database fixture gives a real PostgreSQL and the unprivileged application role;
        // this test only needs it because the host applies migrations and seeds at start-up.
        await _database.InitializeAsync();

        _webRoot = Directory.CreateTempSubdirectory("vetflow-wwwroot-").FullName;
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "index.html"), IndexMarkup);
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "main.js"), "console.log('bundle');");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Standing in for the Docker image's COPY into wwwroot.
            builder.UseSetting(WebHostDefaults.WebRootKey, _webRoot);
            builder.UseSetting("Database:ConnectionString", _database.ConnectionString);
            builder.UseSetting("Database:ApplyMigrationsAtStartup", "true");
            builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
            builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-chars");
        });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _database.DisposeAsync();

        try
        {
            Directory.Delete(_webRoot, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a test over.
        }
    }

    private HttpClient Client => _client ?? throw new InvalidOperationException("Not initialized.");

    [Fact]
    public async Task The_application_shell_is_served_to_a_visitor_with_no_token_REQ_IDN_006()
    {
        var response = await Client.GetAsync(new Uri("/", UriKind.Relative));

        // Anything else and the login screen cannot load — so nobody can ever sign in.
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("vetflow-shell");
    }

    [Fact]
    public async Task The_bundle_is_served_to_a_visitor_with_no_token_REQ_IDN_006()
    {
        var response = await Client.GetAsync(new Uri("/main.js", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_client_side_route_falls_back_to_the_shell_rather_than_404()
    {
        foreach (var route in new[] { "/login", "/catalog/products", "/inventory/history" })
        {
            var response = await Client.GetAsync(new Uri(route, UriKind.Relative));

            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: route);
            (await response.Content.ReadAsStringAsync()).ShouldContain("vetflow-shell", customMessage: route);
        }
    }

    [Fact]
    public async Task Business_endpoints_still_demand_a_token_AC_IDN_005()
    {
        // The exception granted above is the shell and its assets — nothing more.
        var anonymous = await Client.GetAsync(new Uri("/api/v1/products?page=1&pageSize=5", UriKind.Relative));

        anonymous.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await anonymous.Content.ReadAsStringAsync()).ShouldContain("VTF-IDN-002");

        // And they answer normally once signed in, through the same host.
        var token = await ApiFixture.SignInAsync(
            Client, OrganizationSeeder.PilotOwnerPhoneNumber, OrganizationSeeder.PilotOwnerPhoneNumber);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/products?page=1&pageSize=5");
        request.Headers.Add("Authorization", $"Bearer {token}");
        (await Client.SendAsync(request)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task An_unknown_api_route_is_still_a_ProblemDetails_404_not_the_shell()
    {
        var response = await Client.GetAsync(new Uri("/api/v1/does-not-exist", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("vetflow-shell");
    }

    [Fact]
    public async Task Sign_in_works_through_the_same_origin_host_REQ_IDN_002()
    {
        var response = await Client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new { phoneNumber = OrganizationSeeder.PilotOwnerPhoneNumber, password = OrganizationSeeder.PilotOwnerPhoneNumber });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
