using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Inventory;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class ProductOnHandConfiguration : IEntityTypeConfiguration<ProductOnHand>
{
    public void Configure(EntityTypeBuilder<ProductOnHand> builder)
    {
        builder.ScopedToBranch();

        // One on-hand record per product <b>per branch</b> (BR-INV-002 as scoped by BR-ORG-002).
        //
        // This is the single most expensive structure in the system to change once real data
        // exists — a primary key that the whole inventory read path joins, so altering it later
        // means a table rewrite under an exclusive lock plus a rebuild of everything referencing
        // it. On an empty database it is a drop and create. That asymmetry is why ADR-0022 had to
        // be decided before the first real operational entry, and why a future warehouse must be
        // modelled as a Branch rather than a level below one (ADR-0022 §11.1/§12.9): a
        // stock-location level would bring this exact migration back, on live data.
        builder.HasKey(TenantScope.TenantIdProperty, TenantScope.BranchIdProperty, nameof(ProductOnHand.ProductId));
        builder.Property(stock => stock.ProductId).ValueGeneratedNever();
        builder.Property(stock => stock.OnHandQuantity).HasPrecision(18, 3);
    }
}
