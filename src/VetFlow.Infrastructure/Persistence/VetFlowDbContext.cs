using Microsoft.EntityFrameworkCore;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Categories;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new ProductUnitConfiguration());
        modelBuilder.ApplyConfiguration(new ManufacturerConfiguration());
        modelBuilder.ApplyConfiguration(new UnitConfiguration());
        modelBuilder.ApplyConfiguration(new ProductNatureConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());

        SnakeCaseNames.Apply(modelBuilder);
    }
}
