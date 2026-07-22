using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Inventory;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class InventoryBatchConfiguration : IEntityTypeConfiguration<InventoryBatch>
{
    public void Configure(EntityTypeBuilder<InventoryBatch> builder)
    {
        builder.HasKey(batch => batch.Id);

        // The id is assigned by the write kernel (Guid), never by the store.
        builder.Property(batch => batch.Id).ValueGeneratedNever();

        // Cross-module references are stored by id with no FK (the write-kernel owns no navigation;
        // the referenced Catalog product and Purchasing line rows are never deleted) — the Purchasing
        // line-item precedent (BR-PUR-007). Get-only properties are mapped explicitly.
        builder.Property(batch => batch.ProductId).IsRequired();
        builder.Property(batch => batch.PurchaseLineId).IsRequired();
        builder.Property(batch => batch.Quantity).HasPrecision(18, 3);
        builder.Property(batch => batch.RemainingQuantity).HasPrecision(18, 3);
        builder.Property(batch => batch.UnitCostSnapshot).HasPrecision(18, 2);
        builder.Property(batch => batch.ExpiryDate);
        builder.Property(batch => batch.ReceivedAt).IsRequired();

        builder.HasIndex(batch => batch.ProductId);
        builder.HasIndex(batch => batch.PurchaseLineId);
    }
}
