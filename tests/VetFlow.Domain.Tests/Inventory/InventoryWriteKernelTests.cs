using Shouldly;
using VetFlow.Domain.Inventory;

namespace VetFlow.Domain.Tests.Inventory;

/// <summary>
/// The Inventory write kernel domain (write-kernel.md, BR-INV-001/002): a batch initializes its
/// RemainingQuantity to the received quantity (the owner's forward-compat field) and rejects
/// malformed input as a backstop; a product's on-hand starts at zero and only ever increases by a
/// positive amount. Quantities here are already in the canonical stock unit (receiving converts).
/// </summary>
public sealed class InventoryWriteKernelTests
{
    private static readonly DateTimeOffset ReceivedAt = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_batch_sets_remaining_quantity_to_the_received_quantity_BR_INV_001()
    {
        var batch = new InventoryBatch(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            quantity: 240m, unitCostSnapshot: 12.50m, expiryDate: new DateOnly(2027, 1, 1), receivedAt: ReceivedAt);

        batch.Quantity.ShouldBe(240m);
        batch.RemainingQuantity.ShouldBe(240m);
        batch.UnitCostSnapshot.ShouldBe(12.50m);
        batch.ExpiryDate.ShouldBe(new DateOnly(2027, 1, 1));
        batch.ReceivedAt.ShouldBe(ReceivedAt);
    }

    [Fact]
    public void A_batch_may_have_no_expiry_BR_INV_001()
    {
        var batch = new InventoryBatch(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5m, 0m, expiryDate: null, receivedAt: ReceivedAt);

        batch.ExpiryDate.ShouldBeNull();
    }

    [Fact]
    public void A_batch_rejects_a_non_positive_quantity_STD_BE_010()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new InventoryBatch(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, 1m, null, ReceivedAt));
    }

    [Fact]
    public void On_hand_starts_at_zero_and_increases_BR_INV_002()
    {
        var onHand = new ProductOnHand(Guid.NewGuid());
        onHand.OnHandQuantity.ShouldBe(0m);

        onHand.Increase(240m);
        onHand.Increase(60m);

        onHand.OnHandQuantity.ShouldBe(300m);
    }

    [Fact]
    public void On_hand_rejects_a_non_positive_increase_BR_INV_002()
    {
        var onHand = new ProductOnHand(Guid.NewGuid());

        Should.Throw<ArgumentOutOfRangeException>(() => onHand.Increase(0m));
        Should.Throw<ArgumentOutOfRangeException>(() => onHand.Increase(-1m));
    }
}
