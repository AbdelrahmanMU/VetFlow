using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class PurchaseReturnLineConfiguration : IEntityTypeConfiguration<PurchaseReturnLine>
{
    public void Configure(EntityTypeBuilder<PurchaseReturnLine> builder)
    {
        builder.ScopedToBranch();

        builder.HasKey(line => line.Id);

        // The id is assigned by the domain (Guid), never by the store — so a line added to an
        // already-tracked return is an insert, not a spurious update (the sales/purchase-line
        // precedent). Without this the new line is tracked as Modified and SaveChanges issues an
        // UPDATE that matches no row.
        builder.Property(line => line.Id).ValueGeneratedNever();

        builder.Property(line => line.PurchaseLineItemId).IsRequired();
        builder.Property(line => line.ProductId).IsRequired();
        builder.Property(line => line.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(line => line.BatchId).IsRequired();
        // Quantities are never rounded (BR-INV-058, DEC-CAT-033); the precision matches the batch
        // and on-hand columns this line moves, so a quantity cannot lose fidelity in transit.
        builder.Property(line => line.Quantity).HasPrecision(18, 3).IsRequired();
        builder.Property(line => line.AddedAt).IsRequired();

        // Reference-only links to the original line and the batch — no navigation, no cascade
        // (the write-kernel rule): a return line records where stock went back to, and must never
        // be able to delete a batch or a purchase line.

        // BR-PUR-016 sums committed return lines per original purchase line; that is the hot path
        // of the returnable-quantity check, so it is indexed.
        builder.HasIndex(line => line.PurchaseLineItemId);
        builder.HasIndex(line => line.BatchId);
    }
}
