using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VetFlow.Infrastructure.Catalog;
using VetFlow.Infrastructure.Categories;
using VetFlow.Infrastructure.Persistence;
using VetFlow.Infrastructure.Purchasing;

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
        services.AddScoped<ManufacturerListQueryHandler>();
        services.AddScoped<ProductNatureOptionsQueryHandler>();
        services.AddScoped<UnitOptionsQueryHandler>();
        services.AddScoped<CategoryListQueryHandler>();
        services.AddScoped<CreateCategoryCommandHandler>();
        services.AddScoped<RenameCategoryCommandHandler>();
        services.AddScoped<SetCategoryActiveCommandHandler>();
        services.AddScoped<CreateManufacturerCommandHandler>();
        services.AddScoped<RenameManufacturerCommandHandler>();
        services.AddScoped<SetManufacturerActiveCommandHandler>();
        services.AddScoped<PurchaseListQueryHandler>();
        services.AddScoped<PurchaseDetailsQueryHandler>();

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

    /// <summary>
    /// Seeds sample purchase invoices when Database:SeedDevelopmentDataAtStartup is
    /// enabled (development only — DEC-PUR-001). Idempotent and off by default.
    /// </summary>
    public static async Task SeedDevelopmentDataIfConfiguredAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (!options.SeedDevelopmentDataAtStartup)
        {
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<VetFlowDbContext>();
        await PurchaseInvoiceDevelopmentSeeder.SeedAsync(dbContext, TimeProvider.System);
    }
}
