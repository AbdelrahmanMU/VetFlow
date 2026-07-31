using Shouldly;
using VetFlow.Domain.Common;
using VetFlow.Domain.Inventory;

namespace VetFlow.Domain.Tests.Inventory;

/// <summary>
/// The floor rule (BR-INV-061, DEC-INV-032) as the aggregate enforces it. It lives on
/// <see cref="InventoryBatch.ApplyDelta"/> so adjustments, write-off and both returns inherit one
/// guard instead of four — these tests are what stops that guard from being weakened later.
/// </summary>
public sealed class InventoryBatchAdjustmentTests
{
    [Fact]
    public void A_positive_delta_raises_the_remaining_quantity_and_never_the_received_one_BR_INV_061()
    {
        var batch = NewBatch(quantity: 10m);

        batch.ApplyDelta(4m);

        batch.RemainingQuantity.ShouldBe(14m);
        batch.Quantity.ShouldBe(10m);   // the historical received amount never moves
    }

    [Fact]
    public void A_negative_delta_lowers_the_remaining_quantity_BR_INV_061()
    {
        var batch = NewBatch(quantity: 10m);

        batch.ApplyDelta(-4m);

        batch.RemainingQuantity.ShouldBe(6m);
        batch.Quantity.ShouldBe(10m);
    }

    [Fact]
    public void Exactly_emptying_a_batch_is_allowed_and_leaves_it_at_zero_BR_INV_061()
    {
        var batch = NewBatch(quantity: 10m);

        batch.ApplyDelta(-10m);

        // Zero is legal — the batch simply becomes "depleted" by the existing derivation
        // (BR-INV-021). No new batch state is introduced (DEC-INV-011/012).
        batch.RemainingQuantity.ShouldBe(0m);
    }

    [Fact]
    public void A_delta_below_zero_is_rejected_as_a_business_failure_and_changes_nothing_BR_INV_061()
    {
        var batch = NewBatch(quantity: 10m);

        var exception = Should.Throw<BusinessRuleException>(() => batch.ApplyDelta(-10.001m));

        exception.ErrorCode.ShouldBe(InventoryErrorCodes.QuantityBelowZero);
        // Rejected, never clamped to zero and never applied partially (DEC-INV-032).
        batch.RemainingQuantity.ShouldBe(10m);
        exception.Metadata["remaining"].ShouldBe("10");
    }

    [Fact]
    public void A_zero_delta_is_refused_because_it_would_record_nothing()
    {
        var batch = NewBatch(quantity: 10m);

        Should.Throw<ArgumentOutOfRangeException>(() => batch.ApplyDelta(0m));
    }

    [Fact]
    public void On_hand_follows_the_same_delta_so_the_invariant_holds_BR_INV_005()
    {
        var onHand = new ProductOnHand(Guid.NewGuid());
        onHand.Increase(10m);

        onHand.ApplyDelta(-4m);
        onHand.ApplyDelta(1m);

        onHand.OnHandQuantity.ShouldBe(7m);
    }

    [Fact]
    public void The_two_reason_lists_stay_separate_BR_INV_067()
    {
        // The owner ruled two lists, not one vocabulary (DEC-INV-031). A reason that belongs only
        // to write-off must never be accepted by an adjustment, and the reverse.
        InventoryMovementReasons.ForAdjustment.ShouldNotContain(InventoryMovementReason.Expired);
        InventoryMovementReasons.ForAdjustment.ShouldNotContain(InventoryMovementReason.Contaminated);
        InventoryMovementReasons.ForWriteOff.ShouldNotContain(InventoryMovementReason.CountCorrection);
        InventoryMovementReasons.ForWriteOff.ShouldNotContain(InventoryMovementReason.InitialBalance);
        InventoryMovementReasons.ForWriteOff.ShouldNotContain(InventoryMovementReason.Found);

        // And the shared three belong to both, which is why one enum serves two operations.
        foreach (var shared in new[]
                 {
                     InventoryMovementReason.Damaged,
                     InventoryMovementReason.Lost,
                     InventoryMovementReason.Other,
                 })
        {
            InventoryMovementReasons.ForAdjustment.ShouldContain(shared);
            InventoryMovementReasons.ForWriteOff.ShouldContain(shared);
        }
    }

    private static InventoryBatch NewBatch(decimal quantity) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        quantity,
        unitCostSnapshot: 100m,
        expiryDate: null,
        receivedAt: DateTimeOffset.UtcNow);
}
