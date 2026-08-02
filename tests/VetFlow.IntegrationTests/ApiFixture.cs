using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;
using VetFlow.Application.Common;
using VetFlow.Infrastructure.Organization;
using VetFlow.Infrastructure.Persistence;
using VetFlow.Infrastructure.Tenancy;

namespace VetFlow.IntegrationTests;

/// <summary>
/// One real PostgreSQL container + the real API pipeline for the whole test
/// collection (ADR-0016 §3 — integration-first; a mocked database proves
/// nothing about EF Core, transactions, or constraints).
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17-alpine").Build();

    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public HttpClient Client => _client ?? throw new InvalidOperationException("Fixture not initialized.");

    /// <summary>
    /// The application's connection string, for the few tests that need their own
    /// <see cref="VetFlowDbContext"/> — a second connection to prove per-batch concurrency
    /// detection (BR-INV-056), or one with a command interceptor to count the queries an
    /// operation issues (BR-INV-053).
    ///
    /// It is the <b>unprivileged</b> role of <see cref="ApplicationRole"/>, exactly like the
    /// deployed application, so those contexts meet row-level security too.
    /// </summary>
    public string ConnectionString => ApplicationConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await CreateApplicationRoleAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Database:ConnectionString", ApplicationConnectionString());
            builder.UseSetting("Database:ApplyMigrationsAtStartup", "true");
            builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
            builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-chars");
        });

        _client = _factory.CreateClient();

        // Sign in the way a real user does, rather than forging a token. The suite therefore
        // exercises the actual sign-in path, the real claims, and the real filters on every one
        // of its requests — if authentication or tenant resolution breaks, every test fails
        // loudly instead of silently bypassing the thing under test (REQ-IDN-006, BR-IDN-005).
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await SignInAsync(_client, OrganizationSeeder.PilotOwnerPhoneNumber, OrganizationSeeder.PilotOwnerPhoneNumber));
    }

    /// <summary>
    /// The role the tests run the application as. <b>Deliberately not the container's superuser.</b>
    /// PostgreSQL exempts a superuser from row-level security unconditionally — even from a
    /// <c>FORCE</c>d policy — so a suite that ran as one would install the second net of
    /// ADR-0022 §8.2 and then never once execute it. The deployed role is unprivileged (Neon
    /// grants no superuser), and this makes the tests meet the same rules production does.
    /// </summary>
    private const string ApplicationRole = "vetflow_app";

    private const string ApplicationPassword = "vetflow_app";

    private const string ApplicationDatabase = "vetflow";

    private string ApplicationConnectionString() =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Username = ApplicationRole,
            Password = ApplicationPassword,
            Database = ApplicationDatabase,
        }.ConnectionString;

    /// <summary>
    /// Creates that role and a database it owns. Ownership is what lets the migrations create the
    /// schema — and what makes <c>FORCE ROW LEVEL SECURITY</c> meaningful, since a policy binds a
    /// table's owner only when forced.
    /// </summary>
    private async Task CreateApplicationRoleAsync()
    {
        await using var admin = new NpgsqlConnection(_container.GetConnectionString());
        await admin.OpenAsync();

        await using (var role = admin.CreateCommand())
        {
            role.CommandText =
                $"CREATE ROLE {ApplicationRole} LOGIN PASSWORD '{ApplicationPassword}' NOSUPERUSER NOBYPASSRLS";
            await role.ExecuteNonQueryAsync();
        }

        await using var database = admin.CreateCommand();
        database.CommandText = $"CREATE DATABASE {ApplicationDatabase} OWNER {ApplicationRole}";
        await database.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// A client carrying no credentials — for the tests that must act as a <b>different</b> tenant,
    /// or as nobody at all. <see cref="Client"/> is signed in as the seeded owner and cannot prove
    /// either.
    /// </summary>
    public HttpClient CreateAnonymousClient() =>
        (_factory ?? throw new InvalidOperationException("Fixture not initialized.")).CreateClient();

    /// <summary>
    /// Signs in through the real endpoint and returns the access token. Used by the isolation
    /// tests, which must reach the API as a second tenant's user and could not forge a token
    /// without also forging the claims under test.
    /// </summary>
    public static async Task<string> SignInAsync(HttpClient client, string phoneNumber, string password)
    {
        ArgumentNullException.ThrowIfNull(client);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { phoneNumber, password });
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<SignInResponse>()
            ?? throw new InvalidOperationException("Sign-in returned no payload.");

        return payload.AccessToken;
    }

    /// <summary>
    /// Resolves a service from the application's own container inside the given organizational
    /// scope — how a test observes a tenant-resolved component (the clinic clock, AC-ORG-009) as
    /// some tenant other than the seeded one.
    /// </summary>
    public TResult UsingScope<TResult>(Guid tenantId, Guid branchId, Func<IServiceProvider, TResult> use)
    {
        ArgumentNullException.ThrowIfNull(use);

        if (_factory is null)
        {
            throw new InvalidOperationException("Fixture not initialized.");
        }

        using var tenantScope = SystemTenantScope.Begin(tenantId, branchId);
        using var scope = _factory.Services.CreateScope();
        return use(scope.ServiceProvider);
    }

    private sealed record SignInResponse(string AccessToken, DateTimeOffset ExpiresAt, string DisplayName);

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    public async Task SeedAsync(Func<VetFlowDbContext, Task> seed)
    {
        if (_factory is null)
        {
            throw new InvalidOperationException("Fixture not initialized.");
        }

        using var tenantScope = SystemTenantScope.Begin(OrganizationSeeder.PilotScope.TenantId, OrganizationSeeder.PilotScope.BranchId);


        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VetFlowDbContext>();
        await seed(dbContext);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Runs <paramref name="write"/> inside a transaction that is never committed, invokes
    /// <paramref name="whileUncommitted"/> while those rows are still uncommitted, then rolls back.
    /// Because the API request runs on a separate connection under READ COMMITTED, it cannot see the
    /// uncommitted rows — proving read models reflect committed state only (BR-INV-016).
    /// </summary>
    public async Task AssertInvisibleWhileUncommittedAsync(
        Func<VetFlowDbContext, Task> write,
        Func<Task> whileUncommitted)
    {
        if (_factory is null)
        {
            throw new InvalidOperationException("Fixture not initialized.");
        }

        using var tenantScope = SystemTenantScope.Begin(OrganizationSeeder.PilotScope.TenantId, OrganizationSeeder.PilotScope.BranchId);


        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VetFlowDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        await write(dbContext);
        await dbContext.SaveChangesAsync();
        await whileUncommitted();
        await transaction.RollbackAsync();
    }

    /// <summary>
    /// Today at the clinic, resolved through the API's own <see cref="IClinicClock"/> — the exact
    /// basis every expiry decision uses (BR-INV-059/060). A test must never derive it from
    /// <c>DateTime.UtcNow</c>: whenever the clinic's date and the UTC date differ, every
    /// 30-day-horizon boundary case would be measured against the wrong day.
    /// </summary>
    public DateOnly ClinicToday
    {
        get
        {
            if (_factory is null)
            {
                throw new InvalidOperationException("Fixture not initialized.");
            }

            using var tenantScope = SystemTenantScope.Begin(OrganizationSeeder.PilotScope.TenantId, OrganizationSeeder.PilotScope.BranchId);


            using var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<IClinicClock>().Today;
        }
    }

    public async Task<TResult> QueryDbAsync<TResult>(Func<VetFlowDbContext, Task<TResult>> query)
    {
        if (_factory is null)
        {
            throw new InvalidOperationException("Fixture not initialized.");
        }

        using var tenantScope = SystemTenantScope.Begin(OrganizationSeeder.PilotScope.TenantId, OrganizationSeeder.PilotScope.BranchId);


        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VetFlowDbContext>();
        return await query(dbContext);
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
