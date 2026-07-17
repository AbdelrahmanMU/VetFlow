using Shouldly;
using VetFlow.Domain.Purchasing;

namespace VetFlow.Domain.Tests.Purchasing;

/// <summary>
/// The purchase-invoice aggregate (BR-PUR-001 header, BR-PUR-003 status): a
/// header-only document born a draft, refusing to exist without its required
/// fields. The received/cancelled transitions belong to later slices and have no
/// method here yet.
/// </summary>
public sealed class PurchaseInvoiceTests
{
    private static readonly DateOnly InvoiceDate = new(2026, 7, 1);
    private static readonly DateTimeOffset CreatedAt = new(2026, 7, 1, 9, 30, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_invoice_is_a_draft_BR_PUR_003()
    {
        var invoice = NewInvoice();

        invoice.Status.ShouldBe(PurchaseInvoiceStatus.Draft);
    }

    [Fact]
    public void The_constructor_keeps_the_header_fields_BR_PUR_001()
    {
        var invoice = new PurchaseInvoice(
            Guid.NewGuid(),
            "PUR-000001",
            "شركة الدلتا",
            InvoiceDate,
            4250.00m,
            CreatedAt,
            supplierInvoiceReference: "INV-91",
            notes: "دفعة أولى");

        invoice.Number.ShouldBe("PUR-000001");
        invoice.SupplierName.ShouldBe("شركة الدلتا");
        invoice.SupplierInvoiceReference.ShouldBe("INV-91");
        invoice.InvoiceDate.ShouldBe(InvoiceDate);
        invoice.TotalAmount.ShouldBe(4250.00m);
        invoice.Notes.ShouldBe("دفعة أولى");
        invoice.CreatedAt.ShouldBe(CreatedAt);
    }

    [Fact]
    public void The_constructor_trims_the_supplier_name_BR_PUR_001()
    {
        var invoice = NewInvoice(supplierName: "  شركة النور  ");

        invoice.SupplierName.ShouldBe("شركة النور");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void The_constructor_requires_a_supplier_name_BR_PUR_001(string? supplierName)
    {
        Should.Throw<ArgumentException>(() => NewInvoice(supplierName: supplierName!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void The_constructor_requires_a_system_number_BR_PUR_002(string? number)
    {
        Should.Throw<ArgumentException>(() => NewInvoice(number: number!));
    }

    [Fact]
    public void The_constructor_rejects_an_empty_id_BR_PUR_001()
    {
        Should.Throw<ArgumentException>(() => NewInvoice(id: Guid.Empty));
    }

    [Fact]
    public void The_constructor_rejects_a_missing_invoice_date_BR_PUR_001()
    {
        Should.Throw<ArgumentException>(() => new PurchaseInvoice(
            Guid.NewGuid(), "PUR-000001", "شركة الدلتا", default, 100m, CreatedAt));
    }

    [Fact]
    public void The_constructor_rejects_a_negative_total_BR_PUR_001()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => NewInvoice(total: -1m));
    }

    [Fact]
    public void The_constructor_accepts_a_zero_total_for_a_header_only_create_BR_PUR_001()
    {
        // Create (REQ-PUR-003 / DEC-PUR-001) builds the header with a zero total —
        // line items and total derivation belong to a later slice.
        var invoice = NewInvoice(total: 0m);

        invoice.TotalAmount.ShouldBe(0m);
        invoice.Status.ShouldBe(PurchaseInvoiceStatus.Draft);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void The_constructor_normalizes_a_blank_reference_and_notes_to_null_BR_PUR_001(string blank)
    {
        var invoice = new PurchaseInvoice(
            Guid.NewGuid(),
            "PUR-000001",
            "شركة الدلتا",
            InvoiceDate,
            100m,
            CreatedAt,
            supplierInvoiceReference: blank,
            notes: blank);

        invoice.SupplierInvoiceReference.ShouldBeNull();
        invoice.Notes.ShouldBeNull();
    }

    private static PurchaseInvoice NewInvoice(
        Guid? id = null,
        string number = "PUR-000001",
        string supplierName = "شركة الدلتا",
        DateOnly? invoiceDate = null,
        decimal total = 100m) =>
        new(
            id ?? Guid.NewGuid(),
            number,
            supplierName,
            invoiceDate ?? InvoiceDate,
            total,
            CreatedAt);
}
