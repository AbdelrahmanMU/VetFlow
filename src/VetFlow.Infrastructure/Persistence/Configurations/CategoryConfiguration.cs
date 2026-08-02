using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Categories;
using VetFlow.Infrastructure.Categories;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // The catalog belongs to the tenant and is shared across its branches (DEC-ORG-006).
        builder.ScopedToTenant();

        builder.HasKey(category => category.Id);

        // Principal side of Product's composite tenant foreign key (ADR-0022 §12.2), which is
        // what makes a product referencing another tenant's category impossible in the database
        // rather than merely forbidden in code.
        builder.HasAlternateKey(TenantScope.TenantIdProperty, nameof(Category.Id));

        builder.Property(category => category.Name).HasMaxLength(200).IsRequired();
        builder.Property(category => category.IsActive).IsRequired();

        // The normalized search column serves two roles for categories, which have
        // an Arabic name only (BR-CTG-001): fuzzy search (the gin trigram index) and
        // the name-uniqueness key (BR-CTG-003 — the unique btree index below).
        builder.Property<string>(SearchableText.PropertyName)
            .HasMaxLength(SearchableText.MaxLength)
            .IsRequired();
        // Trigram search leads with the tenant (ADR-0022 §5): without it the index scans every
        // clinic's trigrams and the filter discards the rest. `btree_gin` supplies the uuid
        // operator class that lets a GIN index carry the discriminator (DEC-ORG-016).
        builder.HasIndex([TenantScope.TenantIdProperty, SearchableText.PropertyName])
            .HasMethod("gin")
            .HasOperators("uuid_ops", "gin_trgm_ops");
        // Uniqueness leads with the tenant (REQ-ORG-007, AC-ORG-007, ADR-0022 §12.3): two clinics
        // may each have a category called «أدوية», and neither blocks the other's onboarding.
        builder.HasIndex(
                [TenantScope.TenantIdProperty, SearchableText.PropertyName],
                CategoryNameUniqueness.UniqueIndexName)
            .IsUnique();
    }
}
