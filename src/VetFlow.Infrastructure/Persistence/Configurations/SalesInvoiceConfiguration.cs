using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class SalesInvoiceConfiguration : IEntityTypeConfiguration<SalesInvoice>
{
    public void Configure(EntityTypeBuilder<SalesInvoice> builder)
    {
        builder.ScopedToBranch();

        builder.HasKey(invoice => invoice.Id);
        builder.HasAlternateKey(TenantScope.TenantIdProperty, nameof(SalesInvoice.Id));

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
            .HasForeignKey(TenantScope.TenantIdProperty, "SalesInvoiceId")
            .HasPrincipalKey(TenantScope.TenantIdProperty, nameof(SalesInvoice.Id))
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(invoice => invoice.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Trigram search leads with the tenant (ADR-0022 §5): without it the index scans every
        // clinic's trigrams and the filter discards the rest. `btree_gin` supplies the uuid
        // operator class that lets a GIN index carry the discriminator (DEC-ORG-016).
        builder.HasIndex([TenantScope.TenantIdProperty, SearchableText.PropertyName])
            .HasMethod("gin")
            .HasOperators("uuid_ops", "gin_trgm_ops");
        builder.HasIndex(
                [TenantScope.TenantIdProperty, TenantScope.BranchIdProperty, nameof(SalesInvoice.Number)])
            .IsUnique();
        builder.HasIndex(invoice => invoice.Status);
        builder.HasIndex(invoice => invoice.SaleDate);
    }
}
