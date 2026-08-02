using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Categories;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ScopedToTenant();

        builder.HasKey(product => product.Id);
        builder.HasAlternateKey(TenantScope.TenantIdProperty, nameof(Product.Id));

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

        // Composite tenant foreign keys to the other tenant-scoped catalog tables (ADR-0022 §12.2):
        // the tenant travels in the key itself, so a product can never point at another tenant's
        // category or manufacturer. This is enforcement by construction — the invariant does not
        // depend on anyone remembering it.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(TenantScope.TenantIdProperty, nameof(Product.CategoryId))
            .HasPrincipalKey(TenantScope.TenantIdProperty, nameof(Category.Id))
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Manufacturer>()
            .WithMany()
            .HasForeignKey(TenantScope.TenantIdProperty, nameof(Product.ManufacturerId))
            .HasPrincipalKey(TenantScope.TenantIdProperty, nameof(Manufacturer.Id))
            .OnDelete(DeleteBehavior.Restrict);

        // Nature and Unit are platform-global vocabulary, so their keys stay simple.
        builder.HasOne<ProductNature>().WithMany().HasForeignKey(product => product.NatureId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Unit>().WithMany().HasForeignKey(product => product.StorageUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Unit>().WithMany().HasForeignKey(product => product.DefaultSaleUnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Unit>().WithMany().HasForeignKey(product => product.DefaultPurchaseUnitId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(product => product.Units)
            .WithOne()
            .HasForeignKey(TenantScope.TenantIdProperty, "ProductId")
            .HasPrincipalKey(TenantScope.TenantIdProperty, nameof(Product.Id))
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(product => product.Units).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Trigram search leads with the tenant (ADR-0022 §5, DEC-ORG-016).
        builder.HasIndex([TenantScope.TenantIdProperty, SearchableText.PropertyName])
            .HasMethod("gin")
            .HasOperators("uuid_ops", "gin_trgm_ops");
        builder.HasIndex([TenantScope.TenantIdProperty, NormalizedArabicName.PropertyName])
            .HasMethod("gin")
            .HasOperators("uuid_ops", "gin_trgm_ops");
        builder.HasIndex([TenantScope.TenantIdProperty, nameof(Product.InternalCode)]).IsUnique();
        builder.HasIndex(product => product.Status);
    }
}
