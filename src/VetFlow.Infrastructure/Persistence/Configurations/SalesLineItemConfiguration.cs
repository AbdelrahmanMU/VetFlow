using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Sales;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class SalesLineItemConfiguration : IEntityTypeConfiguration<SalesLineItem>
{
    public void Configure(EntityTypeBuilder<SalesLineItem> builder)
    {
        builder.HasKey(line => line.Id);

        // The id is assigned by the domain (Guid), never by the store — so a line added to an
        // already-tracked invoice is an insert, not a spurious update (the purchase-line precedent).
        builder.Property(line => line.Id).ValueGeneratedNever();

        // Get-only properties are not discovered by convention — map them explicitly. The
        // product/unit ids are stored without a cross-module FK; the name and price snapshots make
        // the line self-sufficient for display (BR-SAL-006).
        builder.Property(line => line.ProductId).IsRequired();
        builder.Property(line => line.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(line => line.SaleUnitId).IsRequired();
        builder.Property(line => line.SaleUnitName).HasMaxLength(100).IsRequired();
        builder.Property(line => line.Quantity).HasPrecision(18, 3);
        builder.Property(line => line.UnitPrice).HasPrecision(18, 2);
        builder.Property(line => line.LineTotal).HasPrecision(18, 2);
        builder.Property(line => line.AddedAt).IsRequired();

        builder.HasIndex(line => line.ProductId);
    }
}
