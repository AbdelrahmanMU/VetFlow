using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Catalog;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class ProductNatureConfiguration : IEntityTypeConfiguration<ProductNature>
{
    public void Configure(EntityTypeBuilder<ProductNature> builder)
    {
        builder.HasKey(nature => nature.Id);
        builder.Property(nature => nature.Name).HasMaxLength(200).IsRequired();

        builder.Property<string>(SearchableText.PropertyName)
            .HasMaxLength(SearchableText.MaxLength)
            .IsRequired();
        builder.HasIndex(SearchableText.PropertyName).HasMethod("gin").HasOperators("gin_trgm_ops");

        // The initial extensible nature list (REQ-CAT-011, AC-CAT-011).
        builder.HasData(
            Seed("b1000000-0000-4000-8000-000000000001", "دواء"),
            Seed("b1000000-0000-4000-8000-000000000002", "غذاء"),
            Seed("b1000000-0000-4000-8000-000000000003", "مستلزم طبي"),
            Seed("b1000000-0000-4000-8000-000000000004", "مستلزم حيوانات"),
            Seed("b1000000-0000-4000-8000-000000000005", "منتج عناية"));
    }

    private static object Seed(string id, string name) =>
        new { Id = Guid.Parse(id), Name = name, SearchText = ArabicSearchText.Normalize(name) };
}
