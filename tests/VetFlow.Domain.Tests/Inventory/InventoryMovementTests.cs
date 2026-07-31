using Shouldly;
using VetFlow.Domain.Inventory;

namespace VetFlow.Domain.Tests.Inventory;

/// <summary>
/// The unified movement ledger row (REQ-INV-009, BR-INV-062..067). These replace the Sprint 7
/// InventoryConsumption tests: the record was absorbed by the ledger (DEC-INV-027), and the
/// traceability requirement it served (REQ-INV-008) is now carried by a Consume movement's
/// reference.
/// </summary>
public sealed class InventoryMovementTests
{
    private static readonly DateTimeOffset At = new(2026, 7, 31, 9, 0, 0, TimeSpan.Zero);

    private static InventoryMovement Increase(decimal quantity) =>
        InventoryMovement.Increase(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            InventoryMovementType.Receive, InventoryMovementSource.Purchasing, quantity, At);

    private static InventoryMovement Decrease(decimal quantity) =>
        InventoryMovement.Decrease(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            InventoryMovementType.Consume, InventoryMovementSource.Sales, quantity, At);

    [Fact]
    public void An_increase_is_stored_positive_BR_INV_064()
    {
        Increase(12.5m).Quantity.ShouldBe(12.5m);
    }

    [Fact]
    public void A_decrease_is_stored_negative_so_the_sign_convention_lives_in_one_place_BR_INV_064()
    {
        // The caller passes a magnitude; the direction is the ledger's concern.
        Decrease(12.5m).Quantity.ShouldBe(-12.5m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_movement_rejects_a_non_positive_magnitude(decimal quantity)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Increase(quantity));
        Should.Throw<ArgumentOutOfRangeException>(() => Decrease(quantity));
    }

    [Fact]
    public void A_movement_always_names_its_product_and_batch_BR_INV_064()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => InventoryMovement.Increase(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(),
            InventoryMovementType.Receive, InventoryMovementSource.Purchasing, 1m, At));

        Should.Throw<ArgumentOutOfRangeException>(() => InventoryMovement.Increase(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty,
            InventoryMovementType.Receive, InventoryMovementSource.Purchasing, 1m, At));
    }

    [Fact]
    public void A_consume_movement_carries_the_sale_line_so_traceability_survives_REQ_INV_008()
    {
        var saleLine = Guid.NewGuid();

        var movement = InventoryMovement.Decrease(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            InventoryMovementType.Consume, InventoryMovementSource.Sales, 4m, At, referenceId: saleLine);

        movement.ReferenceId.ShouldBe(saleLine);
        movement.Type.ShouldBe(InventoryMovementType.Consume);
    }

    [Fact]
    public void An_inventory_native_operation_has_no_reference_DEC_INV_036()
    {
        // Adjustments and write-offs have no counterparty document.
        Increase(1m).ReferenceId.ShouldBeNull();
    }

    [Fact]
    public void The_optional_actor_is_trimmed_and_blank_becomes_null_BR_INV_066()
    {
        var named = InventoryMovement.Decrease(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            InventoryMovementType.WriteOff, InventoryMovementSource.Inventory, 1m, At,
            reason: InventoryMovementReason.Expired, actorName: "  الطبيب  ");

        named.ActorName.ShouldBe("الطبيب");

        var anonymous = InventoryMovement.Decrease(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            InventoryMovementType.WriteOff, InventoryMovementSource.Inventory, 1m, At,
            reason: InventoryMovementReason.Expired, actorName: "   ");

        // No users module and no authentication by owner ruling: an absent actor never blocks.
        anonymous.ActorName.ShouldBeNull();
    }

    [Fact]
    public void The_reason_note_is_trimmed_and_blank_becomes_null_BR_INV_067()
    {
        var movement = InventoryMovement.Decrease(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            InventoryMovementType.Adjustment, InventoryMovementSource.Inventory, 1m, At,
            reason: InventoryMovementReason.CountCorrection, reasonNote: "  جرد شهري  ");

        movement.ReasonNote.ShouldBe("جرد شهري");
        movement.Reason.ShouldBe(InventoryMovementReason.CountCorrection);
    }

    [Fact]
    public void The_reason_vocabularies_are_exactly_the_owners_lists_BR_INV_067()
    {
        // DEC-INV-031, verbatim — no term added, none dropped.
        InventoryMovementReasons.ForAdjustment.ShouldBe(
            [
                InventoryMovementReason.CountCorrection,
                InventoryMovementReason.InitialBalance,
                InventoryMovementReason.Damaged,
                InventoryMovementReason.Found,
                InventoryMovementReason.Lost,
                InventoryMovementReason.Other,
            ],
            ignoreOrder: true);

        InventoryMovementReasons.ForWriteOff.ShouldBe(
            [
                InventoryMovementReason.Expired,
                InventoryMovementReason.Damaged,
                InventoryMovementReason.Lost,
                InventoryMovementReason.Contaminated,
                InventoryMovementReason.Other,
            ],
            ignoreOrder: true);
    }

    [Fact]
    public void The_ledger_row_exposes_no_mutator_so_history_can_never_be_edited_DEC_INV_037()
    {
        // Append-only is structural, not a convention: a reviewer changing this would have to
        // add a setter, and this test would fail.
        var writable = typeof(InventoryMovement)
            .GetProperties()
            .Where(property => property.CanWrite)
            .Select(property => property.Name)
            .ToList();

        writable.ShouldBeEmpty();
    }
}
