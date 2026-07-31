using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Sales;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.HasKey(invoice => invoice.Id);

        builder.Property(invoice => invoice.Number).HasMaxLength(50).IsRequired();
        // Optional by ruling (DEC-SAL-002) — deliberately not IsRequired, unlike the supplier name.
        builder.Property(invoice => invoice.CustomerName).HasMaxLength(300);
        builder.Property(invoice => invoice.SaleDate).IsRequired();
        builder.Property(invoice => invoice.TotalAmount).HasPrecision(18, 2);
        builder.Property(invoice => invoice.Notes).HasMaxLength(2000);
        builder.Property(invoice => invoice.CreatedAt).IsRequired();
        builder.Property(invoice => invoice.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // The customer name is searchable with Arabic normalization (BR-SAL-019 —
        // the basic sales list, DEC-SAL-005 ruled 2026-07-31); the trigram index
        // backs the ILIKE partial match. Maintained by SearchTextInterceptor.
        builder.Property<string>(SearchableText.PropertyName)
            .HasMaxLength(SearchableText.MaxLength)
            .IsRequired();

        // Line items are owned by the invoice aggregate (BR-SAL-004): a shadow FK, field access
        // (the collection is private), and cascade delete — the purchase-invoice precedent.
        builder.HasMany(invoice => invoice.Lines)
            .WithOne()
            .HasForeignKey("SalesInvoiceId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(invoice => invoice.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(SearchableText.PropertyName).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(invoice => invoice.Number).IsUnique();
        builder.HasIndex(invoice => invoice.Status);
        builder.HasIndex(invoice => invoice.SaleDate);
    }
}
