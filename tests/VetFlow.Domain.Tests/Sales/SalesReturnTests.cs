using Shouldly;
using VetFlow.Domain.Common;
using VetFlow.Domain.Sales;

namespace VetFlow.Domain.Tests.Sales;

/// <summary>
/// The sales-return aggregate (REQ-SAL-004, DEC-SAL-010): a document born a draft, holding lines
/// that each carry their own quantity against an original sale line, and committing exactly once
/// into immutability (BR-SAL-018).
///
/// <para>What is deliberately <b>not</b> tested here, because the aggregate deliberately does not
/// own it: the returnable ceiling (BR-SAL-016), the Committed-invoice guard (BR-SAL-015), and the
/// distribution across the batches the goods left from (BR-SAL-017) all need data outside the
/// aggregate and are proven in the integration tests.</para>
/// </summary>
public sealed class SalesReturnTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 31, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly ReturnDate = new(2026, 7, 31);

    [Fact]
    public void A_new_return_is_a_draft_with_no_lines_BR_SAL_018()
    {
        var salesReturn = NewReturn();

        salesReturn.Status.ShouldBe(SalesReturnStatus.Draft);
        salesReturn.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void A_return_has_no_cancelled_status_DEC_INV_037()
    {
        // A committed return is corrected by an opposing movement, never cancelled. If a Cancelled
        // member is ever added, this fails — which is the point: the enum is the rule's surface.
        // «ملغاة» on a sales *invoice* is a separate, still-unsettled question (DEC-SAL-009) and
        // gives this enum no third member either.
        Enum.GetNames<SalesReturnStatus>().ShouldBe(["Draft", "Committed"], ignoreOrder: true);
    }

    [Fact]
    public void An_added_line_records_the_original_sale_line_and_the_quantity_BR_SAL_016()
    {
        var salesReturn = NewReturn();
        var originalLineId = Guid.NewGuid();

        var line = salesReturn.AddLine(
            Guid.NewGuid(), originalLineId, Guid.NewGuid(), "أموكسيسيلين", 3m, true, CreatedAt);

        line.SalesLineItemId.ShouldBe(originalLineId);
        line.Quantity.ShouldBe(3m);
        salesReturn.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void A_return_line_carries_no_batch_reference_BR_SAL_013_BR_SAL_017()
    {
        // The one deliberate departure from PurchaseReturnLine. FEFO may have split the sale line
        // across several batches, so a single destination could not express the truth — and Sales
        // may hold no batch reference at all. The destinations are derived at commit and live in the
        // movement ledger. A BatchId appearing here fails this test on purpose.
        typeof(SalesReturnLine)
            .GetProperties()
            .ShouldNotContain(property => property.Name.Contains("Batch", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_non_positive_quantity_is_rejected_BR_SAL_016(decimal quantity)
    {
        var salesReturn = NewReturn();

        var error = Should.Throw<BusinessRuleException>(() => salesReturn.AddLine(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "أموكسيسيلين", quantity, true, CreatedAt));

        error.ErrorCode.ShouldBe(SalesErrorCodes.ReturnLineComposition);
        salesReturn.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void A_fractional_return_of_an_indivisible_product_is_rejected_BR_SAL_016()
    {
        // BR-SAL-016's last clause: a partial return respects the product's splittability exactly as
        // the sale did (DEC-SAL-007). Returning half of an indivisible item is no more possible than
        // selling half of one — rejected, never rounded.
        var salesReturn = NewReturn();

        var error = Should.Throw<BusinessRuleException>(() => salesReturn.AddLine(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "علبة", 2.5m, false, CreatedAt));

        error.ErrorCode.ShouldBe(SalesErrorCodes.ReturnLineComposition);
        salesReturn.Lines.ShouldBeEmpty();

        // The same quantity on a splittable product is accepted — the rule is the product's, not the
        // return's.
        salesReturn.AddLine(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "شراب", 2.5m, true, CreatedAt);
        salesReturn.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void A_removed_line_leaves_the_rest_BR_SAL_018()
    {
        var salesReturn = NewReturn();
        var first = salesReturn.AddLine(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "أ", 2m, true, CreatedAt);
        salesReturn.AddLine(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ب", 5m, true, CreatedAt);

        salesReturn.RemoveLine(first.Id).ShouldBeTrue();

        salesReturn.Lines.Count.ShouldBe(1);
        salesReturn.Lines.ShouldAllBe(line => line.ProductName == "ب");
    }

    [Fact]
    public void Removing_a_line_that_is_not_there_returns_false()
    {
        var salesReturn = NewReturn();

        salesReturn.RemoveLine(Guid.NewGuid()).ShouldBeFalse();
    }

    [Fact]
    public void An_empty_return_cannot_be_committed_BR_SAL_018()
    {
        var salesReturn = NewReturn();

        var error = Should.Throw<BusinessRuleException>(salesReturn.Commit);

        error.ErrorCode.ShouldBe(SalesErrorCodes.ReturnHasNoLines);
        salesReturn.Status.ShouldBe(SalesReturnStatus.Draft);
    }

    [Fact]
    public void Commit_moves_a_draft_with_lines_to_committed_BR_SAL_018()
    {
        var salesReturn = NewReturnWithOneLine();

        salesReturn.Commit();

        salesReturn.Status.ShouldBe(SalesReturnStatus.Committed);
    }

    [Fact]
    public void A_committed_return_cannot_be_committed_again_BR_SAL_018()
    {
        var salesReturn = NewReturnWithOneLine();
        salesReturn.Commit();

        Should.Throw<BusinessRuleException>(salesReturn.Commit)
            .ErrorCode.ShouldBe(SalesErrorCodes.ReturnNotDraft);
    }

    [Fact]
    public void A_committed_return_rejects_every_change_AC_SAL_020()
    {
        var salesReturn = NewReturnWithOneLine();
        var existingLineId = salesReturn.Lines.First().Id;
        salesReturn.Commit();

        Should.Throw<BusinessRuleException>(() => salesReturn.AddLine(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "ج", 1m, true, CreatedAt))
            .ErrorCode.ShouldBe(SalesErrorCodes.ReturnNotDraft);

        Should.Throw<BusinessRuleException>(() => salesReturn.RemoveLine(existingLineId))
            .ErrorCode.ShouldBe(SalesErrorCodes.ReturnNotDraft);

        // The line survived both rejected calls — a rejection changes nothing.
        salesReturn.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void A_return_carries_no_total_DEC_INV_035()
    {
        // A return is a stock movement with no financial effect: no total, no refund, no amount
        // anywhere on the document. The sales *invoice* has a TotalAmount; this deliberately has
        // none, and a cash refund stays out of scope (DEC-SAL-001).
        typeof(SalesReturn)
            .GetProperties()
            .ShouldNotContain(property => property.Name.Contains("Total", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Amount", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void A_blank_customer_snapshot_is_stored_as_null_DEC_SAL_002()
    {
        var salesReturn = new SalesReturn(
            Guid.NewGuid(), "SRT-000001", Guid.NewGuid(), "   ", ReturnDate, CreatedAt);

        salesReturn.CustomerName.ShouldBeNull();
    }

    private static SalesReturn NewReturn() => new(
        Guid.NewGuid(), "SRT-000001", Guid.NewGuid(), "أحمد", ReturnDate, CreatedAt);

    private static SalesReturn NewReturnWithOneLine()
    {
        var salesReturn = NewReturn();
        salesReturn.AddLine(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "أموكسيسيلين", 3m, true, CreatedAt);
        return salesReturn;
    }
}
