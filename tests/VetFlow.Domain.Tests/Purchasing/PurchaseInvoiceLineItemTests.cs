using Shouldly;
using VetFlow.Domain.Common;
using VetFlow.Domain.Purchasing;

namespace VetFlow.Domain.Tests.Purchasing;

/// <summary>
/// Purchase line items inside the invoice aggregate (BR-PUR-005) and the derived
/// invoice total (BR-PUR-006, DEC-PUR-003): the aggregate is the single owner of
/// the total; adding and removing lines recompute it; a line total is
/// quantity × unit price, rounded once to EGP so the header always equals the sum
/// of the displayed lines; the name snapshot is captured at add time (BR-PUR-007).
/// The draft-only guard (BR-PUR-003) is exercised end-to-end in the integration
/// tests, where a non-draft state can be seeded.
/// </summary>
public sealed class PurchaseInvoiceLineItemTests
{
    private static readonly DateTimeOffset AddedAt = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_invoice_has_no_lines_and_a_zero_total_BR_PUR_005()
    {
        var invoice = NewInvoice();

        invoice.Lines.ShouldBeEmpty();
        invoice.TotalAmount.ShouldBe(0m);
    }

    [Fact]
    public void Adding_a_line_computes_the_line_total_and_the_invoice_total_BR_PUR_005()
    {
        var invoice = NewInvoice();

        var line = AddLine(invoice, quantity: 3m, unitPrice: 100m);

        line.LineTotal.ShouldBe(300m);
        invoice.Lines.Count.ShouldBe(1);
        invoice.TotalAmount.ShouldBe(300m);
    }

    [Fact]
    public void The_invoice_total_is_the_sum_of_the_line_totals_TS_PUR_017()
    {
        var invoice = NewInvoice();

        AddLine(invoice, quantity: 2m, unitPrice: 150m); // 300
        AddLine(invoice, quantity: 5m, unitPrice: 20m);  // 100

        invoice.TotalAmount.ShouldBe(400m);
    }

    [Fact]
    public void The_line_total_is_rounded_to_egp_so_the_header_equals_the_sum_of_lines_BR_PUR_006()
    {
        var invoice = NewInvoice();

        // 1.005 × 1.00 = 1.005 → rounds to 1.01 per line; the header must equal 1.01 + 1.01.
        var first = AddLine(invoice, quantity: 1.005m, unitPrice: 1.00m);
        var second = AddLine(invoice, quantity: 1.005m, unitPrice: 1.00m);

        first.LineTotal.ShouldBe(1.01m);
        second.LineTotal.ShouldBe(1.01m);
        invoice.TotalAmount.ShouldBe(2.02m);
        invoice.TotalAmount.ShouldBe(invoice.Lines.Sum(line => line.LineTotal));
    }

    [Fact]
    public void A_line_captures_the_product_and_unit_name_snapshot_BR_PUR_007()
    {
        var invoice = NewInvoice();

        var line = invoice.AddLine(
            Guid.NewGuid(), Guid.NewGuid(), "  أموكسيسيلين  ", Guid.NewGuid(), "  كرتونة  ", 1m, 50m, AddedAt);

        line.ProductName.ShouldBe("أموكسيسيلين");
        line.PurchaseUnitName.ShouldBe("كرتونة");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Adding_a_line_rejects_a_non_positive_quantity_BR_PUR_005(decimal quantity)
    {
        var invoice = NewInvoice();

        var exception = Should.Throw<BusinessRuleException>(() => AddLine(invoice, quantity, unitPrice: 10m));

        exception.ErrorCode.ShouldBe(PurchasingErrorCodes.LineComposition);
        invoice.Lines.ShouldBeEmpty();
        invoice.TotalAmount.ShouldBe(0m);
    }

    [Fact]
    public void Adding_a_line_rejects_a_negative_unit_price_BR_PUR_005()
    {
        var invoice = NewInvoice();

        var exception = Should.Throw<BusinessRuleException>(() => AddLine(invoice, quantity: 1m, unitPrice: -0.01m));

        exception.ErrorCode.ShouldBe(PurchasingErrorCodes.LineComposition);
        invoice.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void A_zero_unit_price_is_allowed_BR_PUR_005()
    {
        var invoice = NewInvoice();

        var line = AddLine(invoice, quantity: 4m, unitPrice: 0m);

        line.LineTotal.ShouldBe(0m);
        invoice.TotalAmount.ShouldBe(0m);
    }

    [Fact]
    public void Removing_a_line_recomputes_the_total_TS_PUR_019()
    {
        var invoice = NewInvoice();
        var first = AddLine(invoice, quantity: 2m, unitPrice: 150m);  // 300
        AddLine(invoice, quantity: 5m, unitPrice: 20m);               // 100

        invoice.RemoveLine(first.Id).ShouldBeTrue();

        invoice.Lines.Count.ShouldBe(1);
        invoice.TotalAmount.ShouldBe(100m);
    }

    [Fact]
    public void Removing_the_last_line_leaves_a_zero_total_BR_PUR_006()
    {
        var invoice = NewInvoice();
        var line = AddLine(invoice, quantity: 3m, unitPrice: 100m);

        invoice.RemoveLine(line.Id).ShouldBeTrue();

        invoice.Lines.ShouldBeEmpty();
        invoice.TotalAmount.ShouldBe(0m);
    }

    [Fact]
    public void Removing_an_unknown_line_changes_nothing_BR_PUR_005()
    {
        var invoice = NewInvoice();
        AddLine(invoice, quantity: 2m, unitPrice: 150m);

        invoice.RemoveLine(Guid.NewGuid()).ShouldBeFalse();

        invoice.Lines.Count.ShouldBe(1);
        invoice.TotalAmount.ShouldBe(300m);
    }

    private static PurchaseLineItem AddLine(PurchaseInvoice invoice, decimal quantity, decimal unitPrice) =>
        invoice.AddLine(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "منتج",
            Guid.NewGuid(),
            "كرتونة",
            quantity,
            unitPrice,
            AddedAt);

    private static PurchaseInvoice NewInvoice() =>
        new(
            Guid.NewGuid(),
            "PUR-000001",
            "شركة الدلتا",
            new DateOnly(2026, 7, 1),
            0m,
            new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero));
}
