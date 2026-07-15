using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VetFlow.Infrastructure.Catalog;
using VetFlow.Infrastructure.Categories;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure;

/// <summary>
/// The Infrastructure layer's single registration extension (ADR-0014 §9),
/// invoked only by the composition root.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Database:ConnectionString must be configured.")
            .ValidateOnStart();

        services.AddSingleton<SearchTextInterceptor>();
        services.AddDbContext<VetFlowDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options
                .UseNpgsql(databaseOptions.ConnectionString)
                .AddInterceptors(serviceProvider.GetRequiredService<SearchTextInterceptor>());
        });

        services.AddScoped<ProductListQueryHandler>();
        services.AddScoped<ProductDetailsQueryHandler>();
        services.AddScoped<PossibleDuplicatesQueryHandler>();
        services.AddScoped<CreateProductCommandHandler>();
        services.AddScoped<UpdateProductCommandHandler>();
        services.AddScoped<ManufacturerOptionsQueryHandler>();
        services.AddScoped<ProductNatureOptionsQueryHandler>();
        services.AddScoped<UnitOptionsQueryHandler>();
        services.AddScoped<CategoryOptionsQueryHandler>();

        return services;
    }

    /// <summary>Applies pending migrations when Database:ApplyMigrationsAtStartup is enabled.</summary>
    public static async Task ApplyMigrationsIfConfiguredAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (!options.ApplyMigrationsAtStartup)
        {
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<VetFlowDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
