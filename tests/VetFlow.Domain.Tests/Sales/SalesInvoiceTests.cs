using Shouldly;
using VetFlow.Domain.Common;
using VetFlow.Domain.Sales;

namespace VetFlow.Domain.Tests.Sales;

/// <summary>
/// The sales-invoice aggregate (REQ-SAL-001): the header (BR-SAL-001), the optional customer
/// (DEC-SAL-002), the draft-born state machine (BR-SAL-003), line composition including the
/// splittability constraint (BR-SAL-004, DEC-SAL-007), the derived total and its rounding
/// (BR-SAL-005/007), and snapshot immutability (BR-SAL-006). The commit transition has its own
/// suite; the inventory effect belongs to the handler and is exercised in integration tests.
/// </summary>
public sealed class SalesInvoiceTests
{
    private static readonly DateTimeOffset AddedAt = new(2026, 7, 30, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void An_invoice_is_born_a_draft_with_a_zero_total_BR_SAL_003()
    {
        var invoice = NewInvoice();

        invoice.Status.ShouldBe(SalesInvoiceStatus.Draft);
        invoice.TotalAmount.ShouldBe(0m);
        invoice.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void The_customer_name_is_optional_and_blank_becomes_absent_DEC_SAL_002()
    {
        NewInvoice(customerName: null).CustomerName.ShouldBeNull();
        NewInvoice(customerName: "   ").CustomerName.ShouldBeNull();
        NewInvoice(customerName: "  أحمد  ").CustomerName.ShouldBe("أحمد");
    }

    [Fact]
    public void The_sale_date_is_required_BR_SAL_001()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new SalesInvoice(
            Guid.NewGuid(), "SAL-000001", default, AddedAt));
    }

    [Fact]
    public void The_total_is_the_sum_of_line_totals_and_follows_the_lines_BR_SAL_005()
    {
        var invoice = NewInvoice();

        AddLine(invoice, quantity: 2m, unitPrice: 50m);
        invoice.TotalAmount.ShouldBe(100m);

        var second = AddLine(invoice, quantity: 1m, unitPrice: 30m);
        invoice.TotalAmount.ShouldBe(130m);

        invoice.RemoveLine(second.Id).ShouldBeTrue();
        invoice.TotalAmount.ShouldBe(100m);
    }

    [Fact]
    public void Removing_the_last_line_returns_the_total_to_zero_BR_SAL_005()
    {
        var invoice = NewInvoice();
        var line = AddLine(invoice, 2m, 50m);

        invoice.RemoveLine(line.Id).ShouldBeTrue();

        invoice.TotalAmount.ShouldBe(0m);
        invoice.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void Removing_a_line_that_is_not_on_the_invoice_changes_nothing_BR_SAL_004()
    {
        var invoice = NewInvoice();
        AddLine(invoice, 2m, 50m);

        invoice.RemoveLine(Guid.NewGuid()).ShouldBeFalse();

        invoice.Lines.Count.ShouldBe(1);
        invoice.TotalAmount.ShouldBe(100m);
    }

    [Fact]
    public void The_line_total_rounds_half_away_from_zero_to_two_decimals_BR_SAL_007()
    {
        var invoice = NewInvoice();

        // 3 × 10.005 = 30.015 → 30.02 away from zero (banker's rounding would give 30.02 too, so
        // the discriminating case is the .125 → .13 below, which banker's would round to .12).
        var line = AddLine(invoice, quantity: 1m, unitPrice: 0.125m);

        line.LineTotal.ShouldBe(0.13m);
        invoice.TotalAmount.ShouldBe(0.13m);
    }

    [Fact]
    public void The_invoice_total_equals_the_sum_of_the_rounded_line_totals_BR_SAL_007()
    {
        var invoice = NewInvoice();
        AddLine(invoice, quantity: 3m, unitPrice: 0.125m);   // 0.375 → 0.38
        AddLine(invoice, quantity: 3m, unitPrice: 0.125m);   // 0.375 → 0.38

        // Rounded once per line, then summed — so the header matches the displayed lines.
        invoice.TotalAmount.ShouldBe(0.76m);
    }

    [Fact]
    public void A_line_quantity_must_be_positive_BR_SAL_004()
    {
        var invoice = NewInvoice();

        Should.Throw<BusinessRuleException>(() => AddLine(invoice, quantity: 0m, unitPrice: 10m))
            .ErrorCode.ShouldBe(SalesErrorCodes.LineComposition);
        Should.Throw<BusinessRuleException>(() => AddLine(invoice, quantity: -1m, unitPrice: 10m))
            .ErrorCode.ShouldBe(SalesErrorCodes.LineComposition);
        invoice.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void A_non_splittable_product_rejects_a_fractional_quantity_DEC_SAL_007()
    {
        var invoice = NewInvoice();

        var error = Should.Throw<BusinessRuleException>(
            () => AddLine(invoice, quantity: 2.5m, unitPrice: 10m, allowsFractionalQuantity: false));

        error.ErrorCode.ShouldBe(SalesErrorCodes.LineComposition);
        error.Metadata["reason"].ShouldBe("quantityNotWholeForNonSplittableProduct");
        // Explicit rejection — nothing is rounded or coerced (owner: "no additional rules").
        invoice.Lines.ShouldBeEmpty();
    }

    [Fact]
    public void A_non_splittable_product_accepts_a_whole_quantity_DEC_SAL_007()
    {
        var invoice = NewInvoice();

        AddLine(invoice, quantity: 3m, unitPrice: 10m, allowsFractionalQuantity: false);

        invoice.Lines.Count.ShouldBe(1);
    }

    [Fact]
    public void A_splittable_product_accepts_a_fractional_quantity_DEC_SAL_007()
    {
        var invoice = NewInvoice();

        var line = AddLine(invoice, quantity: 2.5m, unitPrice: 10m, allowsFractionalQuantity: true);

        line.Quantity.ShouldBe(2.5m);
        line.LineTotal.ShouldBe(25m);
    }

    [Fact]
    public void The_line_keeps_the_snapshots_it_was_given_BR_SAL_006()
    {
        var invoice = NewInvoice();

        var line = invoice.AddLine(
            Guid.NewGuid(), Guid.NewGuid(), "  أموكسيسيلين  ", Guid.NewGuid(), "  علبة  ",
            2m, 50m, allowsFractionalQuantity: true, AddedAt);

        line.ProductName.ShouldBe("أموكسيسيلين");
        line.SaleUnitName.ShouldBe("علبة");
        line.UnitPrice.ShouldBe(50m);
    }

    internal static SalesLineItem AddLine(
        SalesInvoice invoice,
        decimal quantity,
        decimal unitPrice,
        bool allowsFractionalQuantity = true) =>
        invoice.AddLine(
            Guid.NewGuid(), Guid.NewGuid(), "منتج", Guid.NewGuid(), "علبة",
            quantity, unitPrice, allowsFractionalQuantity, AddedAt);

    internal static SalesInvoice NewInvoice(string? customerName = null) =>
        new(
            Guid.NewGuid(),
            "SAL-000001",
            new DateOnly(2026, 7, 30),
            new DateTimeOffset(2026, 7, 30, 9, 0, 0, TimeSpan.Zero),
            customerName);
}
