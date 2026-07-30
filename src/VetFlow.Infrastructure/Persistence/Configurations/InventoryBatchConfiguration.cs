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

        // Concurrency detection at exactly the scope the owner ruled — <b>per batch</b>
        // (BR-INV-056, DEC-INV-023, R6). PostgreSQL's system column `xmin` is used as the row
        // version: EF adds it to the WHERE clause of every UPDATE it issues for a batch, so a sale
        // whose *allocated* batch changed between allocation and commit fails instead of
        // overwriting inventory silently. Two sales on *different* batches of the same product
        // never collide — the false-failure storm a per-product scope would cause. It is a system
        // column: no new field, no DDL, and receiving is unaffected because it only inserts
        // batches (DEC-INV-002 stays as ruled).
        builder.Property<uint>("xmin").IsRowVersion();

        builder.HasIndex(batch => batch.ProductId);
        builder.HasIndex(batch => batch.PurchaseLineId);
    }
}
