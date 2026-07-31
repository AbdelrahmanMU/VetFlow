using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VetFlow.Application.Common;
using VetFlow.Application.Inventory;
using VetFlow.Infrastructure.Catalog;
using VetFlow.Infrastructure.Categories;
using VetFlow.Infrastructure.Common;
using VetFlow.Infrastructure.Inventory;
using VetFlow.Infrastructure.Persistence;
using VetFlow.Infrastructure.Purchasing;
using VetFlow.Infrastructure.Sales;

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

        // The clinic's business date comes from one configured time zone (BR-INV-060). An absent or
        // unresolvable zone refuses to boot: running with an unknown time zone would make the
        // expiry safety decision undefined, and silently falling back to UTC is prohibited.
        services.AddOptions<ClinicTimeOptions>()
            .BindConfiguration(ClinicTimeOptions.SectionName)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.TimeZone),
                "Clinic:TimeZone must be configured (BR-INV-060 — UTC fallback is prohibited).")
            .Validate(IsResolvableTimeZone, "Clinic:TimeZone is not a time zone this system knows.")
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IClinicClock, ClinicClock>();
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
        services.AddScoped<PurchaseLineItemsQueryHandler>();
        services.AddScoped<CreatePurchaseInvoiceCommandHandler>();
        services.AddScoped<AddPurchaseLineItemCommandHandler>();
        services.AddScoped<RemovePurchaseLineItemCommandHandler>();
        services.AddScoped<ReceivePurchaseInvoiceCommandHandler>();
        services.AddScoped<PurchaseReturnableLinesQueryHandler>();
        services.AddScoped<CreatePurchaseReturnCommandHandler>();
        services.AddScoped<AddPurchaseReturnLineCommandHandler>();
        services.AddScoped<RemovePurchaseReturnLineCommandHandler>();
        services.AddScoped<CommitPurchaseReturnCommandHandler>();
        services.AddScoped<InventoryProjectionQueryHandler>();
        services.AddScoped<BatchViewerQueryHandler>();
        services.AddScoped<ExpiryMonitoringQueryHandler>();
        services.AddScoped<InventoryHistoryQueryHandler>();
        services.AddScoped<BatchOperationWriter>();
        services.AddScoped<AdjustInventoryCommandHandler>();
        services.AddScoped<WriteOffInventoryCommandHandler>();
        services.AddScoped<SalesListQueryHandler>();
        services.AddScoped<SalesDetailsQueryHandler>();
        services.AddScoped<SalesLineItemsQueryHandler>();
        services.AddScoped<CreateSalesInvoiceCommandHandler>();
        services.AddScoped<AddSalesLineItemCommandHandler>();
        services.AddScoped<RemoveSalesLineItemCommandHandler>();
        services.AddScoped<CommitSalesInvoiceCommandHandler>();
        services.AddScoped<SalesReturnableLinesQueryHandler>();
        services.AddScoped<CreateSalesReturnCommandHandler>();
        services.AddScoped<AddSalesReturnLineCommandHandler>();
        services.AddScoped<RemoveSalesReturnLineCommandHandler>();
        services.AddScoped<CommitSalesReturnCommandHandler>();

        // Inventory write kernel (write-kernel.md, DEC-INV-001) — the public write contract
        // Purchase Receiving depends on; internals owned by Inventory (DEC-PUR-008).
        services.AddScoped<IInventoryReceiptWriter, InventoryReceiptWriter>();

        // Inventory consumption (REQ-INV-006/007, DEC-INV-019) — the public write contract
        // committing a sale depends on; FEFO and batch selection stay inside Inventory
        // (DEC-SAL-006, BR-SAL-013).
        services.AddScoped<IInventoryConsumptionWriter, InventoryConsumptionWriter>();

        // Inventory sales returns (BR-INV-069, REQ-SAL-004) — the public write contract committing a
        // sales return depends on. Which batches receive the quantity is read from the recorded
        // consumption trace inside Inventory; Sales never sees one (BR-SAL-013, BR-SAL-017).
        services.AddScoped<IInventorySalesReturnWriter, InventorySalesReturnWriter>();

        return services;
    }

    private static bool IsResolvableTimeZone(ClinicTimeOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.TimeZone))
        {
            return false;
        }

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(options.TimeZone);
            return true;
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
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
