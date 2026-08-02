using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class PurchaseInvoiceConfiguration : IEntityTypeConfiguration<PurchaseInvoice>
{
    public void Configure(EntityTypeBuilder<PurchaseInvoice> builder)
    {
        builder.ScopedToBranch();

        builder.HasKey(invoice => invoice.Id);
        builder.HasAlternateKey(TenantScope.TenantIdProperty, nameof(PurchaseInvoice.Id));

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
            .HasForeignKey(TenantScope.TenantIdProperty, "PurchaseInvoiceId")
            .HasPrincipalKey(TenantScope.TenantIdProperty, nameof(PurchaseInvoice.Id))
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(invoice => invoice.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Supplier name + supplier reference are searchable with Arabic normalization
        // (BR-PUR-004); the trigram index backs the ILIKE partial match.
        // Trigram search leads with the tenant (ADR-0022 §5): without it the index scans every
        // clinic's trigrams and the filter discards the rest. `btree_gin` supplies the uuid
        // operator class that lets a GIN index carry the discriminator (DEC-ORG-016).
        builder.HasIndex([TenantScope.TenantIdProperty, SearchableText.PropertyName])
            .HasMethod("gin")
            .HasOperators("uuid_ops", "gin_trgm_ops");
        // Document numbers are unique per branch, not globally (REQ-ORG-007, AC-ORG-008,
        // ADR-0022 §5): two branches may each hold PUR-000001, and neither can repeat its own.
        builder.HasIndex(
                [TenantScope.TenantIdProperty, TenantScope.BranchIdProperty, nameof(PurchaseInvoice.Number)])
            .IsUnique();
        builder.HasIndex(invoice => invoice.Status);
        builder.HasIndex(invoice => invoice.InvoiceDate);
    }
}
