using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Categories;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Name).HasMaxLength(200).IsRequired();

        builder.Property<string>(SearchableText.PropertyName)
            .HasMaxLength(SearchableText.MaxLength)
            .IsRequired();
        builder.HasIndex(SearchableText.PropertyName).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
