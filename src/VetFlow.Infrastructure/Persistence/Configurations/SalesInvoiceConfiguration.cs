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

        // No search column: Sprint 7 has no sales list to search (DEC-SAL-005 undecided, list not
        // invented), so there is nothing for SearchTextInterceptor to maintain.

        // Line items are owned by the invoice aggregate (BR-SAL-004): a shadow FK, field access
        // (the collection is private), and cascade delete — the purchase-invoice precedent.
        builder.HasMany(invoice => invoice.Lines)
            .WithOne()
            .HasForeignKey("SalesInvoiceId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(invoice => invoice.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(invoice => invoice.Number).IsUnique();
        builder.HasIndex(invoice => invoice.Status);
        builder.HasIndex(invoice => invoice.SaleDate);
    }
}
