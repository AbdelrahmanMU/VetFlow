using Shouldly;
using VetFlow.Domain.Common;
using VetFlow.Domain.Purchasing;

namespace VetFlow.Domain.Tests.Purchasing;

/// <summary>
/// The purchase-return aggregate (REQ-PUR-006, DEC-PUR-010): a document born a draft, holding lines
/// that each carry their own quantity and the batch their original purchase line created, and
/// committing exactly once into immutability (BR-PUR-018).
///
/// <para>What is deliberately <b>not</b> tested here, because the aggregate deliberately does not
/// own it: the returnable ceiling (BR-PUR-016) and the Received-invoice guard (BR-PUR-015) both
/// need data outside the aggregate and are proven in the integration tests.</para>
/// </summary>
public sealed class PurchaseReturnTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 31, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly ReturnDate = new(2026, 7, 31);

    [Fact]
    public void A_new_return_is_a_draft_with_no_lines_BR_PUR_018()
    {
        var purchaseReturn = NewReturn();

        purchaseReturn.Status.ShouldBe(PurchaseReturnStatus.Draft);
        purchaseReturn.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void A_return_has_no_cancelled_status_DEC_INV_037()
    {
        // A committed return is corrected by an opposing movement, never cancelled. If a Cancelled
        // member is ever added, this fails — which is the point: the enum is the rule's surface.
        Enum.GetNames<PurchaseReturnStatus>().ShouldBe(["Draft", "Committed"], ignoreOrder: true);
    }

    [Fact]
    public void An_added_line_records_the_batch_and_the_quantity_BR_PUR_017()
    {
        var purchaseReturn = NewReturn();
        var batchId = Guid.NewGuid();
        var originalLineId = Guid.NewGuid();

        var line = purchaseReturn.AddLine(
            Guid.NewGuid(), originalLineId, Guid.NewGuid(), "أموكسيسيلين", batchId, 3m, CreatedAt);

        line.BatchId.ShouldBe(batchId);
        line.PurchaseLineItemId.ShouldBe(originalLineId);
        line.Quantity.ShouldBe(3m);
        purchaseReturn.Lines.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_non_positive_quantity_is_rejected_BR_PUR_016(decimal quantity)
    {
        var purchaseReturn = NewReturn();

        var error = Should.Throw<BusinessRuleException>(() => purchaseReturn.AddLine(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "أموكسيسيلين", Guid.NewGuid(), quantity, CreatedAt));

        error.ErrorCode.ShouldBe(PurchasingErrorCodes.ReturnLineComposition);
        purchaseReturn.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void A_removed_line_leaves_the_rest_BR_PUR_018()
    {
        var purchaseReturn = NewReturn();
        var first = purchaseReturn.AddLine(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "أ", Guid.NewGuid(), 2m, CreatedAt);
        purchaseReturn.AddLine(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ب", Guid.NewGuid(), 5m, CreatedAt);

        purchaseReturn.RemoveLine(first.Id).ShouldBeTrue();

        purchaseReturn.Lines.Count.ShouldBe(1);
        purchaseReturn.Lines.ShouldAllBe(line => line.ProductName == "ب");
    }

    [Fact]
    public void Removing_a_line_that_is_not_there_returns_false()
    {
        var purchaseReturn = NewReturn();

        purchaseReturn.RemoveLine(Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    public void An_empty_return_cannot_be_committed_BR_PUR_018()
    {
        var purchaseReturn = NewReturn();

        var error = Should.Throw<BusinessRuleException>(purchaseReturn.Commit);

        error.ErrorCode.ShouldBe(PurchasingErrorCodes.ReturnHasNoLines);
        purchaseReturn.Status.ShouldBe(PurchaseReturnStatus.Draft);
    }

    [Fact]
    public void Commit_moves_a_draft_with_lines_to_committed_BR_PUR_018()
    {
        var purchaseReturn = NewReturnWithOneLine();

        purchaseReturn.Commit();

        purchaseReturn.Status.ShouldBe(PurchaseReturnStatus.Committed);
    }

    [Fact]
    public void A_committed_return_cannot_be_committed_again_BR_PUR_018()
    {
        var purchaseReturn = NewReturnWithOneLine();
        purchaseReturn.Commit();

        Should.Throw<BusinessRuleException>(purchaseReturn.Commit)
            .ErrorCode.ShouldBe(PurchasingErrorCodes.ReturnNotDraft);
    }

    [Fact]
    public void A_committed_return_rejects_every_change_AC_PUR_025()
    {
        var purchaseReturn = NewReturnWithOneLine();
        var existingLineId = purchaseReturn.Lines.First().Id;
        purchaseReturn.Commit();

        Should.Throw<BusinessRuleException>(() => purchaseReturn.AddLine(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ج", Guid.NewGuid(), 1m, CreatedAt))
            .ErrorCode.ShouldBe(PurchasingErrorCodes.ReturnNotDraft);

        Should.Throw<BusinessRuleException>(() => purchaseReturn.RemoveLine(existingLineId))
            .ErrorCode.ShouldBe(PurchasingErrorCodes.ReturnNotDraft);

        // The line survived both rejected calls — a rejection changes nothing.
        purchaseReturn.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void A_blank_supplier_snapshot_is_stored_as_null_BR_PUR_001()
    {
        var purchaseReturn = new PurchaseReturn(
            Guid.NewGuid(), "PRT-000001", Guid.NewGuid(), "   ", ReturnDate, CreatedAt);

        purchaseReturn.SupplierName.ShouldBeNull();
    }

    private static PurchaseReturn NewReturn() => new(
        Guid.NewGuid(), "PRT-000001", Guid.NewGuid(), "مورّد الأدوية", ReturnDate, CreatedAt);

    private static PurchaseReturn NewReturnWithOneLine()
    {
        var purchaseReturn = NewReturn();
        purchaseReturn.AddLine(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "أموكسيسيلين", Guid.NewGuid(), 3m, CreatedAt);
        return purchaseReturn;
    }
}
