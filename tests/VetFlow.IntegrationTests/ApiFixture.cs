using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<VetFlowDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        await write(dbContext);
        await dbContext.SaveChangesAsync();
        await whileUncommitted();
        await transaction.RollbackAsync();
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
