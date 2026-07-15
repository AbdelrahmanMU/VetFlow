using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Catalog;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class ProductUnitConfiguration : IEntityTypeConfiguration<ProductUnit>
{
    private const int BarcodeMaxLength = 100;

    public void Configure(EntityTypeBuilder<ProductUnit> builder)
    {
        builder.HasKey(productUnit => productUnit.Id);

        // The id is assigned by the domain (Guid), never by the store. Declaring this
        // lets EF treat a unit added to an already-tracked product (an edit that grows
        // or replaces the profile) as an insert, not a spurious update (DEC-CAT-031).
        builder.Property(productUnit => productUnit.Id).ValueGeneratedNever();

        // Get-only properties are not discovered by convention — map them explicitly.
        builder.Property(productUnit => productUnit.Position);
        builder.Property(productUnit => productUnit.IsPurchaseUnit);
        builder.Property(productUnit => productUnit.IsSaleUnit);
        builder.Property(productUnit => productUnit.QuantityInNextUnit).HasPrecision(18, 6);
        builder.Property(productUnit => productUnit.SellingPrice).HasPrecision(12, 2);
        builder.Property(productUnit => productUnit.Barcode).HasMaxLength(BarcodeMaxLength);

        builder.HasOne<Unit>().WithMany().HasForeignKey(productUnit => productUnit.UnitId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(productUnit => productUnit.Barcode);
    }
}
