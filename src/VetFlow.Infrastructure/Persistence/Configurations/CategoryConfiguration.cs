using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Categories;
using VetFlow.Infrastructure.Categories;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(200).IsRequired();
        builder.Property(category => category.IsActive).IsRequired();

        // The normalized search column serves two roles for categories, which have
        // an Arabic name only (BR-CTG-001): fuzzy search (the gin trigram index) and
        // the name-uniqueness key (BR-CTG-003 — the unique btree index below).
        builder.Property<string>(SearchableText.PropertyName)
            .HasMaxLength(SearchableText.MaxLength)
            .IsRequired();
        builder.HasIndex(SearchableText.PropertyName).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex([SearchableText.PropertyName], CategoryNameUniqueness.UniqueIndexName).IsUnique();
    }
}
