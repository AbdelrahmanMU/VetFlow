using Microsoft.EntityFrameworkCore;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Categories;
using VetFlow.Domain.Inventory;
using VetFlow.Domain.Purchasing;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence.Configurations;

namespace VetFlow.Infrastructure.Persistence;

public sealed class VetFlowDbContext(DbContextOptions<VetFlowDbContext> options) : DbContext(options)
{
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new ProductUnitConfiguration());
        modelBuilder.ApplyConfiguration(new ManufacturerConfiguration());
        modelBuilder.ApplyConfiguration(new UnitConfiguration());
        modelBuilder.ApplyConfiguration(new ProductNatureConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
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

        SnakeCaseNames.Apply(modelBuilder);
    }
}
