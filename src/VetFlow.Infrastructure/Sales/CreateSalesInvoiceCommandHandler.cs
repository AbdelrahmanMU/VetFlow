using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Sales.Commands.CreateSalesInvoice;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Create-sales-invoice write path (REQ-SAL-001) — Sales' first command. It allocates the number
/// from the PostgreSQL sequence (BR-SAL-002 — unique and ascending under concurrency), builds the
/// aggregate through the domain constructor (born a draft with a zero total, every BR-SAL invariant
/// enforced there — STD-BE-010), and persists it in a single <c>SaveChanges</c> (STD-BE-024).
/// A literal mirror of the create-purchase-invoice handler. <b>No inventory effect</b>: a draft
/// reserves nothing and decrements nothing (BR-SAL-004/010).
/// </summary>
public sealed class CreateSalesInvoiceCommandHandler(VetFlowDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<CreateSalesInvoiceCommand, CreateSalesInvoiceResult>
{
    public async Task<CreateSalesInvoiceResult> HandleAsync(
        CreateSalesInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        var sequenceValue = await dbContext.Database
            .SqlQueryRaw<long>($"SELECT nextval('{InternalSalesInvoiceNumber.SequenceName}') AS \"Value\"")
            .SingleAsync(cancellationToken);
        var number = InternalSalesInvoiceNumber.Format(sequenceValue);

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

        return new CreateSalesInvoiceResult { Id = invoice.Id, Number = invoice.Number };
    }
}
