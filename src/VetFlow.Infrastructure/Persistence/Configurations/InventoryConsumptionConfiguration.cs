using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Inventory;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class InventoryConsumptionConfiguration : IEntityTypeConfiguration<InventoryConsumption>
{
    public void Configure(EntityTypeBuilder<InventoryConsumption> builder)
    {
        builder.HasKey(consumption => consumption.Id);

        // The id is assigned by the consumption writer (Guid), never by the store.
        builder.Property(consumption => consumption.Id).ValueGeneratedNever();

        // Get-only properties are mapped explicitly. The sale line is stored by id with no
        // cross-module FK — exactly as InventoryBatch stores PurchaseLineId (BR-PUR-010): the data
        // is Inventory's, and Sales stays unaware of batches (BR-SAL-013).
        builder.Property(consumption => consumption.BatchId).IsRequired();
        builder.Property(consumption => consumption.ProductId).IsRequired();
        builder.Property(consumption => consumption.SaleLineId).IsRequired();
        builder.Property(consumption => consumption.Quantity).HasPrecision(18, 3);
        builder.Property(consumption => consumption.ConsumedAt).IsRequired();

        // The two directions traceability must answer (REQ-INV-008, AC-INV-047): sale line → the
        // batches it consumed, and batch → what consumed it.
        builder.HasIndex(consumption => consumption.SaleLineId);
        builder.HasIndex(consumption => consumption.BatchId);
    }
}
