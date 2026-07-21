using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Purchasing;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.HasKey(invoice => invoice.Id);

        builder.Property(invoice => invoice.Number).HasMaxLength(50).IsRequired();
        builder.Property(invoice => invoice.SupplierName).HasMaxLength(300).IsRequired();
        builder.Property(invoice => invoice.SupplierInvoiceReference).HasMaxLength(100);
        builder.Property(invoice => invoice.InvoiceDate).IsRequired();
        builder.Property(invoice => invoice.TotalAmount).HasPrecision(18, 2);
        builder.Property(invoice => invoice.Notes).HasMaxLength(2000);
        builder.Property(invoice => invoice.CreatedAt).IsRequired();
        builder.Property(invoice => invoice.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property<string>(SearchableText.PropertyName)
            .HasMaxLength(SearchableText.MaxLength)
            .IsRequired();

        // Line items are owned by the invoice aggregate (BR-PUR-005, DEC-PUR-003): a
        // shadow FK, field access (the collection is private), and cascade delete — the
        // Product/ProductUnit precedent.
        builder.HasMany(invoice => invoice.Lines)
            .WithOne()
            .HasForeignKey("PurchaseInvoiceId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(invoice => invoice.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Supplier name + supplier reference are searchable with Arabic normalization
        // (BR-PUR-004); the trigram index backs the ILIKE partial match.
        builder.HasIndex(SearchableText.PropertyName).HasMethod("gin").HasOperators("gin_trgm_ops");
        builder.HasIndex(invoice => invoice.Number).IsUnique();
        builder.HasIndex(invoice => invoice.Status);
        builder.HasIndex(invoice => invoice.InvoiceDate);
    }
}
