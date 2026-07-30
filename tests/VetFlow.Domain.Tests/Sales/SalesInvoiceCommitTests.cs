using Shouldly;
using VetFlow.Domain.Common;
using VetFlow.Domain.Sales;

namespace VetFlow.Domain.Tests.Sales;

/// <summary>
/// The commit transition on the sales-invoice aggregate (REQ-SAL-003, BR-SAL-009/011/012): a draft
/// with at least one line transitions to Committed; an empty draft or an already-committed invoice
/// is rejected without mutation; and afterwards the invoice is fully immutable (BR-SAL-011,
/// AC-SAL-010) — there is no path back to Draft and no undo. The inventory effect (BR-SAL-010)
/// belongs to the handler and is exercised in the integration tests, where batches can be seeded.
/// </summary>
public sealed class SalesInvoiceCommitTests
{
    [Fact]
    public void Committing_a_draft_with_lines_transitions_to_committed_BR_SAL_009()
    {
        var invoice = SalesInvoiceTests.NewInvoice();
        SalesInvoiceTests.AddLine(invoice, 2m, 50m);

        invoice.Commit();

        invoice.Status.ShouldBe(SalesInvoiceStatus.Committed);
    }

    [Fact]
    public void Committing_an_empty_invoice_is_rejected_AC_SAL_008()
    {
        var invoice = SalesInvoiceTests.NewInvoice();

        var error = Should.Throw<BusinessRuleException>(() => invoice.Commit());

        error.ErrorCode.ShouldBe(SalesErrorCodes.InvoiceHasNoLines);
        invoice.Status.ShouldBe(SalesInvoiceStatus.Draft);
    }

    [Fact]
    public void Committing_twice_is_rejected_AC_SAL_008()
    {
        var invoice = SalesInvoiceTests.NewInvoice();
        SalesInvoiceTests.AddLine(invoice, 2m, 50m);
        invoice.Commit();

        Should.Throw<BusinessRuleException>(() => invoice.Commit())
            .ErrorCode.ShouldBe(SalesErrorCodes.InvoiceNotDraft);
    }

    [Fact]
    public void A_committed_invoice_is_immutable_AC_SAL_010()
    {
        var invoice = SalesInvoiceTests.NewInvoice();
        var line = SalesInvoiceTests.AddLine(invoice, 2m, 50m);
        invoice.Commit();

        Should.Throw<BusinessRuleException>(() => SalesInvoiceTests.AddLine(invoice, 1m, 10m))
            .ErrorCode.ShouldBe(SalesErrorCodes.InvoiceNotDraft);
        Should.Throw<BusinessRuleException>(() => invoice.RemoveLine(line.Id))
            .ErrorCode.ShouldBe(SalesErrorCodes.InvoiceNotDraft);

        invoice.TotalAmount.ShouldBe(100m);
        invoice.Lines.Count.ShouldBe(1);
    }
}
