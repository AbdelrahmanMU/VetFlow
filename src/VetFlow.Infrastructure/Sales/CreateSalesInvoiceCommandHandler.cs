using VetFlow.Application.Common;
using VetFlow.Application.Sales.Commands.CreateSalesInvoice;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;
using VetFlow.Infrastructure.Persistence.Numbering;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Create-sales-invoice write path (REQ-SAL-001) — Sales' first command. It allocates the number
/// from its branch's counter (BR-SAL-002 — unique and ascending under concurrency; ADR-0022 §6), builds the
/// aggregate through the domain constructor (born a draft with a zero total, every BR-SAL invariant
/// enforced there — STD-BE-010), and persists it in a single <c>SaveChanges</c> (STD-BE-024).
/// A literal mirror of the create-purchase-invoice handler. <b>No inventory effect</b>: a draft
/// reserves nothing and decrements nothing (BR-SAL-004/010).
/// </summary>
public sealed class CreateSalesInvoiceCommandHandler(
    VetFlowDbContext dbContext,
    DocumentNumbers documentNumbers,
    TimeProvider timeProvider)
    : ICommandHandler<CreateSalesInvoiceCommand, CreateSalesInvoiceResult>
{
    public async Task<CreateSalesInvoiceResult> HandleAsync(
        CreateSalesInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        // One transaction around the allocation and the insert: a failed save returns the number
        // rather than burning it (ADR-0022 §6 — gapless by owner ruling).
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var number = InternalSalesInvoiceNumber.Format(
            await documentNumbers.NextAsync(DocumentSeries.SalesInvoice, cancellationToken));

        // The validator guarantees SaleDate is present before the handler runs; the customer name
        // is optional by ruling (DEC-SAL-002).
        var invoice = new SalesInvoice(
            Guid.NewGuid(),
            number,
            command.SaleDate!.Value,
            timeProvider.GetUtcNow(),
            command.CustomerName,
            command.Notes);

        dbContext.SalesInvoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CreateSalesInvoiceResult { Id = invoice.Id, Number = invoice.Number };
    }
}
