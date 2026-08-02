using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetFlow.Domain.Inventory;
using VetFlow.Infrastructure.Persistence.Attribution;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.Infrastructure.Persistence.Configurations;

/// <summary>
/// The movement ledger's storage (REQ-INV-009). Append-only by construction: the entity exposes no
/// mutator, so EF only ever issues INSERTs against this table (DEC-INV-037).
/// </summary>
public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ScopedToBranch();

        // Every movement records the signed-in user who wrote it (BR-INV-066 as amended
        // 2026-08-02, REQ-IDN-008). Derived from the token's claims, never passed in.
        builder.AttributedToUser();

        builder.HasKey(movement => movement.Id);

        // The id is assigned by the writer (Guid), never by the store.
        builder.Property(movement => movement.Id).ValueGeneratedNever();

        builder.Property(movement => movement.ProductId).IsRequired();
        builder.Property(movement => movement.BatchId).IsRequired();
        builder.Property(movement => movement.Type).IsRequired();
        builder.Property(movement => movement.Source).IsRequired();

        // Signed, in the product's canonical stock unit, at the same precision as every other
        // inventory quantity (BR-INV-002/064). Never rounded (BR-INV-058).
        builder.Property(movement => movement.Quantity).HasPrecision(18, 3);

        // Null for inventory-native operations, which have no counterparty document
        // (DEC-INV-036). No cross-module FK — the InventoryBatch.PurchaseLineId precedent.
        builder.Property(movement => movement.ReferenceId);

        builder.Property(movement => movement.Reason);
        builder.Property(movement => movement.ReasonNote).HasMaxLength(500);

        // A historical field since the 2026-08-02 amendment: values recorded before authentication
        // existed stay readable exactly as they are, and are neither deleted nor rewritten. It is
        // simply no longer the source of attribution — the stamped performer above is.
        builder.Property(movement => movement.ActorName).HasMaxLength(200);

        builder.Property(movement => movement.OccurredAt).IsRequired();

        // History is ordered newest-first with a stable tiebreak (the preserved BR-INV-044).
        builder.HasIndex(movement => new { movement.OccurredAt, movement.Id });

        // Batch → its movements, and the traceability REQ-INV-008 needs: a sale line's movements
        // are found by reference (this index replaces the absorbed InventoryConsumption's two).
        builder.HasIndex(movement => movement.BatchId);
        builder.HasIndex(movement => movement.ReferenceId);
        builder.HasIndex(movement => movement.ProductId);
    }
}
