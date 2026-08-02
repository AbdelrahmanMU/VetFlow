using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Catalog;
using VetFlow.Infrastructure.Catalog;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        // Tenant-scoped, not platform-global (DEC-ORG-005). A centrally curated manufacturer list
        // is a product feature, not an architectural requirement; if it is ever wanted it arrives
        // as a global catalogue tenants IMPORT FROM, which avoids both cross-tenant writes and a
        // later cross-tenant deduplication.
        builder.ScopedToTenant();

        builder.HasKey(manufacturer => manufacturer.Id);
        builder.HasAlternateKey(TenantScope.TenantIdProperty, nameof(Manufacturer.Id));
        builder.Property(manufacturer => manufacturer.Name).HasMaxLength(200).IsRequired();
        builder.Property(manufacturer => manufacturer.IsActive).IsRequired();

        // The normalized search column serves two roles for manufacturers, which have
        // an Arabic name only (BR-CAT-007): fuzzy search (the gin trigram index) and
        // the name-uniqueness key (the unique btree index below).
        builder.Property<string>(SearchableText.PropertyName)
            .HasMaxLength(SearchableText.MaxLength)
            .IsRequired();
        // Trigram search leads with the tenant (ADR-0022 §5): without it the index scans every
        // clinic's trigrams and the filter discards the rest. `btree_gin` supplies the uuid
        // operator class that lets a GIN index carry the discriminator (DEC-ORG-016).
        builder.HasIndex([TenantScope.TenantIdProperty, SearchableText.PropertyName])
            .HasMethod("gin")
            .HasOperators("uuid_ops", "gin_trgm_ops");
        builder.HasIndex(
                [TenantScope.TenantIdProperty, SearchableText.PropertyName],
                ManufacturerNameUniqueness.UniqueIndexName)
            .IsUnique();
    }
}
