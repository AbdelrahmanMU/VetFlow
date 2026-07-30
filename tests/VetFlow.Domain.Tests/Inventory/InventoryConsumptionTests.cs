using Shouldly;
using VetFlow.Domain.Inventory;

namespace VetFlow.Domain.Tests.Inventory;

/// <summary>
/// The write half of consumption on the Inventory entities (REQ-INV-006): the batch decrement and
/// the on-hand decrease (BR-INV-047), the traceability record (BR-INV-057), and the guards that
/// keep quantities from ever going negative. FEFO ordering, the saleable predicate, sufficiency,
/// and atomicity are exercised where they live — in the allocator's integration tests.
/// </summary>
public sealed class InventoryConsumptionTests
{
    private static readonly DateTimeOffset ReceivedAt = new(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Consuming_reduces_only_the_remaining_quantity_BR_INV_047()
    {
        var batch = NewBatch(quantity: 50m);

        batch.Consume(12m);

        batch.RemainingQuantity.ShouldBe(38m);
        // The received quantity is a historical value and never changes.
        batch.Quantity.ShouldBe(50m);
    }

    [Fact]
    public void Consuming_the_whole_remainder_leaves_exactly_zero_BR_INV_047()
    {
        var batch = NewBatch(quantity: 50m);

        batch.Consume(50m);

        batch.RemainingQuantity.ShouldBe(0m);
    }

    [Fact]
    public void Consuming_more_than_remains_is_refused_BR_INV_047()
    {
        var batch = NewBatch(quantity: 50m);

        Should.Throw<ArgumentOutOfRangeException>(() => batch.Consume(51m));

        batch.RemainingQuantity.ShouldBe(50m);
    }

    [Fact]
    public void Consuming_a_non_positive_quantity_is_refused_BR_INV_046()
    {
        var batch = NewBatch(quantity: 50m);

        Should.Throw<ArgumentOutOfRangeException>(() => batch.Consume(0m));
        Should.Throw<ArgumentOutOfRangeException>(() => batch.Consume(-1m));
    }

    [Fact]
    public void On_hand_decreases_by_the_consumed_amount_BR_INV_047()
    {
        var onHand = new ProductOnHand(Guid.NewGuid());
        onHand.Increase(50m);

        onHand.Decrease(12m);

        onHand.OnHandQuantity.ShouldBe(38m);
    }

    [Fact]
    public void On_hand_never_goes_negative_BR_INV_005()
    {
        var onHand = new ProductOnHand(Guid.NewGuid());
        onHand.Increase(10m);

        Should.Throw<ArgumentOutOfRangeException>(() => onHand.Decrease(11m));

        onHand.OnHandQuantity.ShouldBe(10m);
    }

    [Fact]
    public void A_consumption_record_requires_the_sale_line_it_belongs_to_BR_INV_046()
    {
        // Traceability information is a precondition of acceptance, not an optional extra
        // (REQ-INV-008): a quantity that cannot be attributed to its line is never consumed.
        Should.Throw<ArgumentOutOfRangeException>(() => new InventoryConsumption(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 5m, ReceivedAt));
    }

    [Fact]
    public void A_consumption_record_keeps_the_batch_line_and_quantity_BR_INV_057()
    {
        var batchId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var saleLineId = Guid.NewGuid();

        var consumption = new InventoryConsumption(
            Guid.NewGuid(), batchId, productId, saleLineId, 20m, ReceivedAt);

        consumption.BatchId.ShouldBe(batchId);
        consumption.ProductId.ShouldBe(productId);
        consumption.SaleLineId.ShouldBe(saleLineId);
        consumption.Quantity.ShouldBe(20m);
        consumption.ConsumedAt.ShouldBe(ReceivedAt);
    }

    private static InventoryBatch NewBatch(decimal quantity) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), quantity, 100m, null, ReceivedAt);
}
