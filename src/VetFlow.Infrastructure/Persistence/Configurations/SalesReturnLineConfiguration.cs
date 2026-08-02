using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

public sealed class SalesReturnLineConfiguration : IEntityTypeConfiguration<SalesReturnLine>
{
    public void Configure(EntityTypeBuilder<SalesReturnLine> builder)
    {
        builder.ScopedToBranch();

        builder.HasKey(line => line.Id);

        // The id is assigned by the domain (Guid), never by the store — so a line added to an
        // already-tracked return is an insert, not a spurious update (the sales/purchase-line
        // precedent). Without this the new line is tracked as Modified and SaveChanges issues an
        // UPDATE that matches no row.
        builder.Property(line => line.Id).ValueGeneratedNever();

        builder.Property(line => line.SalesLineItemId).IsRequired();
        builder.Property(line => line.ProductId).IsRequired();
        builder.Property(line => line.ProductName).HasMaxLength(300).IsRequired();
        // Quantities are never rounded (BR-INV-058, DEC-CAT-033); the precision matches the sale
        // line this quantity is capped against, and the batch columns it eventually moves.
        builder.Property(line => line.Quantity).HasPrecision(18, 3).IsRequired();
        builder.Property(line => line.AddedAt).IsRequired();

        // There is deliberately no batch column here at all — unlike purchase_return_lines. One sale
        // line may have left through several batches (BR-SAL-017) and Sales may hold no batch
        // reference (BR-SAL-013); the destinations live in the movement ledger, written at commit.

        // Reference-only link to the original sale line — no navigation, no cascade (the
        // write-kernel rule). BR-SAL-016 sums committed return lines per original sale line; that is
        // the hot path of the returnable-quantity check, so it is indexed.
        builder.HasIndex(line => line.SalesLineItemId);
    }
}
