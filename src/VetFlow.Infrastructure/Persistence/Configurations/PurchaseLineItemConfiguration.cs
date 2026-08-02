using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class PurchaseLineItemConfiguration : IEntityTypeConfiguration<PurchaseLineItem>
{
    public void Configure(EntityTypeBuilder<PurchaseLineItem> builder)
    {
        builder.ScopedToBranch();

        builder.HasKey(line => line.Id);

        // The id is assigned by the domain (Guid), never by the store — so a line added to
        // an already-tracked invoice is an insert, not a spurious update (the ProductUnit
        // precedent, DEC-CAT-031).
        builder.Property(line => line.Id).ValueGeneratedNever();

        // Get-only properties are not discovered by convention — map them explicitly.
        // The product/unit ids are stored for future linkage (DEC-PUR-003); there is no
        // cross-module FK — the name snapshot makes the line self-sufficient for display
        // (BR-PUR-007), and the referenced catalog rows are never deleted.
        builder.Property(line => line.ProductId).IsRequired();
        builder.Property(line => line.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(line => line.PurchaseUnitId).IsRequired();
        builder.Property(line => line.PurchaseUnitName).HasMaxLength(100).IsRequired();
        builder.Property(line => line.Quantity).HasPrecision(18, 3);
        builder.Property(line => line.UnitPrice).HasPrecision(18, 2);
        builder.Property(line => line.LineTotal).HasPrecision(18, 2);
        builder.Property(line => line.AddedAt).IsRequired();

        builder.HasIndex(line => line.ProductId);
    }
}
