using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Inventory;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class ProductOnHandConfiguration : IEntityTypeConfiguration<ProductOnHand>
{
    public void Configure(EntityTypeBuilder<ProductOnHand> builder)
    {
        // One on-hand record per product — the product id is the natural key (BR-INV-002).
        builder.HasKey(stock => stock.ProductId);
        builder.Property(stock => stock.ProductId).ValueGeneratedNever();
        builder.Property(stock => stock.OnHandQuantity).HasPrecision(18, 3);
    }
}
