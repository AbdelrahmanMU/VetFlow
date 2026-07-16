using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Catalog;
using VetFlow.Infrastructure.Catalog;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        builder.HasKey(manufacturer => manufacturer.Id);
        builder.Property(manufacturer => manufacturer.Name).HasMaxLength(200).IsRequired();
        builder.Property(manufacturer => manufacturer.IsActive).IsRequired();

        // The normalized search column serves two roles for manufacturers, which have
        // an Arabic name only (BR-CAT-007): fuzzy search (the gin trigram index) and
        // the name-uniqueness key (the unique btree index below).
        builder.Property<string>(SearchableText.PropertyName)
            .HasMaxLength(SearchableText.MaxLength)
            .IsRequired();
        builder.HasIndex(SearchableText.PropertyName).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex([SearchableText.PropertyName], ManufacturerNameUniqueness.UniqueIndexName).IsUnique();
    }
}
