using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using VetFlow.Infrastructure.Persistence;

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

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Database:ConnectionString", _container.GetConnectionString());
            builder.UseSetting("Database:ApplyMigrationsAtStartup", "true");
            builder.UseSetting("Serilog:MinimumLevel:Default", "Warning");
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

        await _container.DisposeAsync();
    }

    public async Task SeedAsync(Func<VetFlowDbContext, Task> seed)
    {
        if (_factory is null)
        {
            throw new InvalidOperationException("Fixture not initialized.");
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VetFlowDbContext>();
        await seed(dbContext);
        await dbContext.SaveChangesAsync();
    }

    public async Task<TResult> QueryDbAsync<TResult>(Func<VetFlowDbContext, Task<TResult>> query)
    {
        if (_factory is null)
        {
            throw new InvalidOperationException("Fixture not initialized.");
        }

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
