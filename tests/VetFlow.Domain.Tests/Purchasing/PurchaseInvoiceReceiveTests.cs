using Shouldly;
using VetFlow.Domain.Common;
using VetFlow.Domain.Purchasing;

namespace VetFlow.Domain.Tests.Purchasing;

/// <summary>
/// The receive transition on the invoice aggregate (REQ-PUR-005, BR-PUR-009/011/012): a draft with
/// at least one line transitions to Received; an empty draft or a non-draft invoice is rejected
/// without mutation; and after receiving the invoice is immutable — the Draft-only guards block any
/// further line change (BR-PUR-011, AC-PUR-017). The inventory effect (BR-PUR-010) belongs to the
/// handler and is exercised in the integration tests, where products can be seeded.
/// </summary>
public sealed class PurchaseInvoiceReceiveTests
{
    private static readonly DateTimeOffset AddedAt = new(2026, 7, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Receiving_a_draft_with_lines_transitions_to_received_BR_PUR_009()
    {
        var invoice = NewInvoice();
        AddLine(invoice, 2m, 100m);

        invoice.Receive();

        invoice.Status.ShouldBe(PurchaseInvoiceStatus.Received);
    }

    [Fact]
    public void Receiving_an_empty_invoice_is_rejected_AC_PUR_016()
    {
        var invoice = NewInvoice();

        var error = Should.Throw<BusinessRuleException>(() => invoice.Receive());

        error.ErrorCode.ShouldBe(PurchasingErrorCodes.InvoiceHasNoLines);
        invoice.Status.ShouldBe(PurchaseInvoiceStatus.Draft);
    }

    [Fact]
    public void Receiving_an_already_received_invoice_is_rejected_AC_PUR_015()
    {
        var invoice = NewInvoice();
        AddLine(invoice, 2m, 100m);
        invoice.Receive();

        var error = Should.Throw<BusinessRuleException>(() => invoice.Receive());

        error.ErrorCode.ShouldBe(PurchasingErrorCodes.InvoiceNotDraft);
    }

    [Fact]
    public void A_received_invoice_is_immutable_AC_PUR_017()
    {
        var invoice = NewInvoice();
        var line = AddLine(invoice, 2m, 100m);
        invoice.Receive();

        Should.Throw<BusinessRuleException>(() => AddLine(invoice, 1m, 10m))
            .ErrorCode.ShouldBe(PurchasingErrorCodes.InvoiceNotDraft);
        Should.Throw<BusinessRuleException>(() => invoice.RemoveLine(line.Id))
            .ErrorCode.ShouldBe(PurchasingErrorCodes.InvoiceNotDraft);
    }

    private static PurchaseLineItem AddLine(PurchaseInvoice invoice, decimal quantity, decimal unitPrice) =>
        invoice.AddLine(Guid.NewGuid(), Guid.NewGuid(), "منتج", Guid.NewGuid(), "كرتونة", quantity, unitPrice, AddedAt);

    private static PurchaseInvoice NewInvoice() =>
        new(
            Guid.NewGuid(),
            "PUR-000001",
            "شركة الدلتا",
            new DateOnly(2026, 7, 1),
            0m,
            new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero));
}
