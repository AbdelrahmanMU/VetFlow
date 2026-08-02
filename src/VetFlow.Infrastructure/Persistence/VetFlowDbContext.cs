using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Categories;
using VetFlow.Domain.Identity;
using VetFlow.Domain.Inventory;
using VetFlow.Domain.Organization;
using VetFlow.Domain.Purchasing;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence.Configurations;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence;

public sealed class VetFlowDbContext(
    DbContextOptions<VetFlowDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    /// <summary>
    /// The organizational scope every read is filtered by and every write is stamped with
    /// (ADR-0022 §3). It is a <b>singleton that resolves per access</b> — see
    /// <see cref="TenantScope.ApplyReadFilters"/> for why a scoped instance here would leak the
    /// first request's tenant into EF's cached model and serve it to every later request.
    /// </summary>
    private readonly ITenantContext _tenantContext = tenantContext;

    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();

    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();

    public DbSet<Unit> Units => Set<Unit>();

    public DbSet<ProductNature> ProductNatures => Set<ProductNature>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();

    public DbSet<PurchaseLineItem> PurchaseLineItems => Set<PurchaseLineItem>();

    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();

    public DbSet<PurchaseReturnLine> PurchaseReturnLines => Set<PurchaseReturnLine>();

    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();

    public DbSet<ProductOnHand> ProductOnHands => Set<ProductOnHand>();

    /// <summary>
    /// The unified movement ledger (REQ-INV-009). It records history and never calculates
    /// inventory — the authoritative quantities stay in <see cref="InventoryBatches"/> and
    /// <see cref="ProductOnHands"/> (BR-INV-063). It absorbed the Sprint 7 InventoryConsumption
    /// record rather than duplicating that state (DEC-INV-027).
    /// </summary>
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();

    public DbSet<SalesLineItem> SalesLineItems => Set<SalesLineItem>();

    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();

    public DbSet<SalesReturnLine> SalesReturnLines => Set<SalesReturnLine>();

    /// <summary>
    /// The organizational foundation (ADR-0022). These four are deliberately unfiltered —
    /// see <see cref="TenantConfiguration"/> for why sign-in could not otherwise read the
    /// membership that establishes the scope in the first place.
    /// </summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<Branch> Branches => Set<Branch>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");

        // Lets a GIN trigram index carry the tenant discriminator as its leading column
        // (ADR-0022 §5, DEC-ORG-016). A trusted extension, so the unprivileged application role
        // can create it — verified before adopting it.
        modelBuilder.HasPostgresExtension("btree_gin");

        // Principals before dependents. A composite tenant foreign key (ADR-0022 §12.2) names the
        // principal's discriminator, so the principal must have declared it first — otherwise EF
        // tries to invent an untyped shadow property on the principal and fails.
        modelBuilder.ApplyConfiguration(new UnitConfiguration());
        modelBuilder.ApplyConfiguration(new ProductNatureConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ManufacturerConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new ProductUnitConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseLineItemConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseReturnConfiguration());
        modelBuilder.ApplyConfiguration(new PurchaseReturnLineConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryBatchConfiguration());
        modelBuilder.ApplyConfiguration(new ProductOnHandConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryMovementConfiguration());
        modelBuilder.ApplyConfiguration(new SalesInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new SalesLineItemConfiguration());
        modelBuilder.ApplyConfiguration(new SalesReturnConfiguration());
        modelBuilder.ApplyConfiguration(new SalesReturnLineConfiguration());

        // Numbering (ADR-0022 §6). Mapped like every other table so that it is migrated, scoped
        // and covered by the isolation tests — even though nothing reads or writes it through EF.
        modelBuilder.ApplyConfiguration(new DocumentCounterConfiguration());

        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        modelBuilder.ApplyConfiguration(new BranchConfiguration());
        modelBuilder.ApplyConfiguration(new MembershipConfiguration());
        modelBuilder.ApplyConfiguration(new UserConfiguration());

        // Applied once, after every configuration, so that no configuration can forget it and
        // no future entity can quietly ship without a filter (REQ-ORG-006, ADR-0022 §12.7).
        TenantScope.ApplyReadFilters(modelBuilder, _tenantContext);

        SnakeCaseNames.Apply(modelBuilder);
    }
}
