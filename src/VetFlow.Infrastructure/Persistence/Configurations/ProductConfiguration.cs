using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Categories;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);

        builder.Property(product => product.InternalCode).HasMaxLength(50).IsRequired();
        builder.Property(product => product.ArabicName).HasMaxLength(300).IsRequired();
        builder.Property(product => product.EnglishName).HasMaxLength(300);
        builder.Property(product => product.Size).HasMaxLength(100);
        builder.Property(product => product.Concentration).HasMaxLength(100);
        builder.Property(product => product.InternalNotes).HasMaxLength(2000);
        builder.Property(product => product.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property<string>(SearchableText.PropertyName)
            .HasMaxLength(SearchableText.MaxLength)
            .IsRequired();

        builder.Property<string>(NormalizedArabicName.PropertyName)
            .HasMaxLength(NormalizedArabicName.MaxLength)
            .IsRequired();

        builder.HasOne<Category>().WithMany().HasForeignKey(product => product.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Manufacturer>().WithMany().HasForeignKey(product => product.ManufacturerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductNature>().WithMany().HasForeignKey(product => product.NatureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Unit>().WithMany().HasForeignKey(product => product.StorageUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Unit>().WithMany().HasForeignKey(product => product.DefaultSaleUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Unit>().WithMany().HasForeignKey(product => product.DefaultPurchaseUnitId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(product => product.Units)
            .WithOne()
            .HasForeignKey("ProductId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(product => product.Units).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(SearchableText.PropertyName).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(NormalizedArabicName.PropertyName).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(product => product.InternalCode).IsUnique();
        builder.HasIndex(product => product.Status);
    }
}
