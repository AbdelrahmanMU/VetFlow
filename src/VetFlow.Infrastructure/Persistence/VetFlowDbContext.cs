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

    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();

    public DbSet<ProductOnHand> ProductOnHands => Set<ProductOnHand>();

    public DbSet<InventoryConsumption> InventoryConsumptions => Set<InventoryConsumption>();

    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();

    public DbSet<SalesLineItem> SalesLineItems => Set<SalesLineItem>();

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
        modelBuilder.ApplyConfiguration(new InventoryBatchConfiguration());
        modelBuilder.ApplyConfiguration(new ProductOnHandConfiguration());
        modelBuilder.ApplyConfiguration(new InventoryConsumptionConfiguration());
        modelBuilder.ApplyConfiguration(new SalesInvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new SalesLineItemConfiguration());

        SnakeCaseNames.Apply(modelBuilder);
    }
}
