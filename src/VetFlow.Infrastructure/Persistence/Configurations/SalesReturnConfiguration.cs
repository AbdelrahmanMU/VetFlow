using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class SalesReturnConfiguration : IEntityTypeConfiguration<SalesReturn>
{
    public void Configure(EntityTypeBuilder<SalesReturn> builder)
    {
        builder.ScopedToBranch();

        builder.HasKey(salesReturn => salesReturn.Id);
        builder.HasAlternateKey(TenantScope.TenantIdProperty, nameof(SalesReturn.Id));

        builder.Property(salesReturn => salesReturn.Number).HasMaxLength(50).IsRequired();
        // Optional, because the customer name it snapshots is itself optional free text (DEC-SAL-002).
        builder.Property(salesReturn => salesReturn.CustomerName).HasMaxLength(300);
        builder.Property(salesReturn => salesReturn.ReturnDate).IsRequired();
        builder.Property(salesReturn => salesReturn.Notes).HasMaxLength(2000);
        builder.Property(salesReturn => salesReturn.CreatedAt).IsRequired();
        builder.Property(salesReturn => salesReturn.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // The originating invoice is a reference, never a cascade: deleting a sales invoice must not
        // silently erase the returns written against it (the write-kernel reference-only rule).
        builder.Property(salesReturn => salesReturn.SalesInvoiceId).IsRequired();

        // Lines are owned by the aggregate (BR-SAL-018): shadow FK, field access, cascade delete —
        // the sales-invoice and purchase-return precedent.
        builder.HasMany(salesReturn => salesReturn.Lines)
            .WithOne()
            .HasForeignKey(TenantScope.TenantIdProperty, "SalesReturnId")
            .HasPrincipalKey(TenantScope.TenantIdProperty, nameof(SalesReturn.Id))
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(salesReturn => salesReturn.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(
                [TenantScope.TenantIdProperty, TenantScope.BranchIdProperty, nameof(SalesReturn.Number)])
            .IsUnique();
        builder.HasIndex(salesReturn => salesReturn.Status);
        // The returnable-quantity check (BR-SAL-016) reads every committed return for one invoice,
        // so that lookup gets an index rather than a sequential scan as returns accumulate.
        builder.HasIndex(salesReturn => salesReturn.SalesInvoiceId);
    }
}
