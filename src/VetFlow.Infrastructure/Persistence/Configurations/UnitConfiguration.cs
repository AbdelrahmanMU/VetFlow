using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Catalog;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.HasKey(unit => unit.Id);
        builder.Property(unit => unit.Name).HasMaxLength(100).IsRequired();

        // The ready-made default unit set (REQ-CAT-016, BR-CAT-017).
        builder.HasData(
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000001"), Name = "قطعة" },
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000002"), Name = "علبة" },
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000003"), Name = "كرتونة" },
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000004"), Name = "شريط" },
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000005"), Name = "قرص" },
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000006"), Name = "زجاجة" },
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000007"), Name = "سم" },
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000008"), Name = "مل" },
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000009"), Name = "لتر" },
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000010"), Name = "جرام" },
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000011"), Name = "كيلوجرام" },
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000012"), Name = "شيكارة" },
            new { Id = Guid.Parse("a1000000-0000-4000-8000-000000000013"), Name = "متر" });
    }
}
