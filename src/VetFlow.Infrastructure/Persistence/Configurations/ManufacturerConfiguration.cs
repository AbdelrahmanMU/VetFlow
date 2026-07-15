using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Catalog;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class ManufacturerConfiguration : IEntityTypeConfiguration<Manufacturer>
{
    public void Configure(EntityTypeBuilder<Manufacturer> builder)
    {
        builder.HasKey(manufacturer => manufacturer.Id);
        builder.Property(manufacturer => manufacturer.Name).HasMaxLength(200).IsRequired();

        builder.Property<string>(SearchableText.PropertyName)
            .HasMaxLength(SearchableText.MaxLength)
            .IsRequired();
        builder.HasIndex(SearchableText.PropertyName).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
