using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class PurchaseReturnConfiguration : IEntityTypeConfiguration<PurchaseReturn>
{
    public void Configure(EntityTypeBuilder<PurchaseReturn> builder)
    {
        builder.ScopedToBranch();

        builder.HasKey(purchaseReturn => purchaseReturn.Id);
        builder.HasAlternateKey(TenantScope.TenantIdProperty, nameof(PurchaseReturn.Id));

        builder.Property(purchaseReturn => purchaseReturn.Number).HasMaxLength(50).IsRequired();
        // Optional, because the supplier name it snapshots is itself optional free text (BR-PUR-001).
        builder.Property(purchaseReturn => purchaseReturn.SupplierName).HasMaxLength(300);
        builder.Property(purchaseReturn => purchaseReturn.ReturnDate).IsRequired();
        builder.Property(purchaseReturn => purchaseReturn.Notes).HasMaxLength(2000);
        builder.Property(purchaseReturn => purchaseReturn.CreatedAt).IsRequired();
        builder.Property(purchaseReturn => purchaseReturn.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // The originating invoice is a reference, never a cascade: deleting a purchase invoice must
        // not silently erase the returns written against it (the write-kernel reference-only rule).
        builder.Property(purchaseReturn => purchaseReturn.PurchaseInvoiceId).IsRequired();

        // Lines are owned by the aggregate (BR-PUR-018): shadow FK, field access, cascade delete —
        // the purchase-invoice and sales-invoice precedent.
        builder.HasMany(purchaseReturn => purchaseReturn.Lines)
            .WithOne()
            .HasForeignKey(TenantScope.TenantIdProperty, "PurchaseReturnId")
            .HasPrincipalKey(TenantScope.TenantIdProperty, nameof(PurchaseReturn.Id))
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(purchaseReturn => purchaseReturn.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(
                [TenantScope.TenantIdProperty, TenantScope.BranchIdProperty, nameof(PurchaseReturn.Number)])
            .IsUnique();
        builder.HasIndex(purchaseReturn => purchaseReturn.Status);
        // The returnable-quantity check (BR-PUR-016) reads every committed return for one invoice,
        // so that lookup gets an index rather than a sequential scan as returns accumulate.
        builder.HasIndex(purchaseReturn => purchaseReturn.PurchaseInvoiceId);
    }
}
