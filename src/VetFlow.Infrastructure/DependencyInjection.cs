using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VetFlow.Application.Common;
using VetFlow.Application.Identity;
using VetFlow.Application.Inventory;
using VetFlow.Infrastructure.Catalog;
using VetFlow.Infrastructure.Categories;
using VetFlow.Infrastructure.Common;
using VetFlow.Infrastructure.Dashboard;
using VetFlow.Infrastructure.Identity;
using VetFlow.Infrastructure.Inventory;
using VetFlow.Infrastructure.Organization;
using VetFlow.Infrastructure.Persistence;
using VetFlow.Infrastructure.Persistence.Attribution;
using VetFlow.Infrastructure.Persistence.Numbering;
using VetFlow.Infrastructure.Persistence.Tenancy;
using VetFlow.Infrastructure.Purchasing;
using VetFlow.Infrastructure.Sales;
using VetFlow.Infrastructure.Tenancy;

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

        // The zone a newly seeded clinic starts with (DEC-ORG-007 moved the running source onto the
        // tenant). An absent or unresolvable zone still refuses to boot: seeding a clinic with a
        // zone nobody checked would make the expiry safety decision undefined from its first day,
        // and silently falling back to UTC is prohibited (BR-INV-060, BR-ORG-007).
        services.AddOptions<ClinicTimeOptions>()
            .BindConfiguration(ClinicTimeOptions.SectionName)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.TimeZone),
                "Clinic:TimeZone must be configured (BR-INV-060 — UTC fallback is prohibited).")
            .Validate(IsResolvableTimeZone, "Clinic:TimeZone is not a time zone this system knows.")
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);

        // The clock is tenant-resolved (DEC-ORG-007, AC-ORG-009) and singleton like the tenant
        // context it reads: it resolves the current request's tenant on every access, so one
        // instance is correct for every clinic instead of for one.
        services.AddSingleton<TenantTimeZones>();
        services.AddSingleton<IClinicClock, ClinicClock>();
        services.AddSingleton<SearchTextInterceptor>();

        // Singleton, matching the interceptor beside it and the tenant context it depends on.
        // Reads are filtered by the model; writes are stamped here — one mechanism per direction,
        // neither of them a parameter any handler passes (BR-ORG-003, ADR-0022 §12.5).
        services.AddSingleton<TenantStampInterceptor>();

        // The database-level second net (ADR-0022 §8.2): it publishes the tenant to the session
        // that the row-level-security policies read. Singleton for the same reason as the two
        // above — it resolves the scope per connection, not per registration.
        services.AddSingleton<TenantSessionInterceptor>();

        // Attribution (REQ-IDN-008, BR-INV-066 as amended): every movement records the signed-in
        // user, stamped here rather than passed by a writer.
        services.AddSingleton<ActorStampInterceptor>();

        services.AddDbContext<VetFlowDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options
                .UseNpgsql(databaseOptions.ConnectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<SearchTextInterceptor>(),
                    serviceProvider.GetRequiredService<TenantStampInterceptor>(),
                    serviceProvider.GetRequiredService<ActorStampInterceptor>(),
                    serviceProvider.GetRequiredService<TenantSessionInterceptor>());
        });

        // Document numbering (ADR-0022 §6). Scoped, because it allocates inside the caller's
        // DbContext and the caller's transaction — which is what makes the series gapless.
        services.AddScoped<DocumentNumbers>();

        services.AddSingleton<IPasswordHasher, PasswordHasherAdapter>();
        services.AddScoped<SignInCommandHandler>();
        services.AddScoped<OrganizationSeeder>();

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
        services.AddScoped<ProductInventorySummaryQueryHandler>();
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

        // The three module-owned dashboard reads (REQ-INV-013 / REQ-SAL-006 / REQ-PUR-007) and
        // the composition over them (REQ-DSH-010). The composer depends on the *decorated*
        // query handlers, so each owning read still passes through validation and logging.
        services.AddScoped<InventoryDashboardSummaryQueryHandler>();
        services.AddScoped<SalesDashboardSummaryQueryHandler>();
        services.AddScoped<PurchasingDashboardSummaryQueryHandler>();
        services.AddScoped<OperationalDashboardQueryHandler>();

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
    /// Creates the Pilot clinic if it is not already there (ADR-0022 §10). Runs after migrations
    /// and before the first request, because the application has no anonymous path: without a
    /// tenant, a branch and an owner there is nobody who could sign in.
    /// </summary>
    public static async Task SeedOrganizationAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var clinicTime = scope.ServiceProvider.GetRequiredService<IOptions<ClinicTimeOptions>>().Value;
        var seeder = scope.ServiceProvider.GetRequiredService<OrganizationSeeder>();

        // The tenant's own time zone starts as the configured one, so the Pilot's clinic date is
        // byte-identical to what it was before (BR-INV-060 unchanged, DEC-ORG-007 — the source
        // moved, the rule did not).
        await seeder.SeedAsync(clinicTime.TimeZone);
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

        // Development data belongs to the seeded clinic like any other row. Seeding runs before
        // anyone has signed in, so the scope is stated explicitly here rather than resolved from
        // a principal that does not exist yet — the one legitimate use of SystemTenantScope
        // alongside bootstrap and tests.
        using var tenantScope = SystemTenantScope.Begin(
            OrganizationSeeder.PilotScope.TenantId,
            OrganizationSeeder.PilotScope.BranchId);

        var dbContext = scope.ServiceProvider.GetRequiredService<VetFlowDbContext>();
        var documentNumbers = scope.ServiceProvider.GetRequiredService<DocumentNumbers>();
        await PurchaseInvoiceDevelopmentSeeder.SeedAsync(dbContext, documentNumbers, TimeProvider.System);
    }
}
