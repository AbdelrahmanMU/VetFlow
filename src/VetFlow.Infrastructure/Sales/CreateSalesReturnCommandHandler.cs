using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Sales.Commands.CreateSalesReturn;
using VetFlow.Domain.Common;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// Create-sales-return write path (REQ-SAL-004, AC-SAL-014). It allocates the <c>SRT-</c> number
/// from its PostgreSQL sequence (BR-SAL-014 — the same mechanism as <c>SAL-</c>, not a second one),
/// snapshots the customer from the originating invoice, and persists the draft in a single
/// <c>SaveChanges</c> (STD-BE-024).
///
/// <para>Returns <c>null</c> when the invoice does not exist (404). An invoice that is not
/// <b>Committed</b> is rejected with VTF-SAL-015 (BR-SAL-015 → 409, AC-SAL-015): a draft never
/// consumed stock, so there is no consumption trace to return along — the return could only ever
/// fail later, and rejecting here makes it early and legible instead of late and cryptic.</para>
/// </summary>
public sealed class CreateSalesReturnCommandHandler(VetFlowDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<CreateSalesReturnCommand, CreateSalesReturnResult?>
{
    public async Task<CreateSalesReturnResult?> HandleAsync(
        CreateSalesReturnCommand command,
        CancellationToken cancellationToken)
    {
        // The validator guarantees both are present before the handler runs.
        var invoiceId = command.SalesInvoiceId!.Value;

        var invoice = await dbContext.SalesInvoices
            .FirstOrDefaultAsync(item => item.Id == invoiceId, cancellationToken);

        if (invoice is null)
        {
            return null;
        }

        if (invoice.Status != SalesInvoiceStatus.Committed)
        {
            throw new BusinessRuleException(
                SalesErrorCodes.ReturnOriginalInvoiceNotCommitted,
                new Dictionary<string, string> { ["status"] = invoice.Status.ToString() });
        }

        var sequenceValue = await dbContext.Database
            .SqlQueryRaw<long>($"SELECT nextval('{InternalSalesReturnNumber.SequenceName}') AS \"Value\"")
            .SingleAsync(cancellationToken);
        var number = InternalSalesReturnNumber.Format(sequenceValue);

        var salesReturn = new SalesReturn(
            Guid.NewGuid(),
            number,
            invoice.Id,
            invoice.CustomerName,
            command.ReturnDate!.Value,
            timeProvider.GetUtcNow(),
            command.Notes);

        dbContext.SalesReturns.Add(salesReturn);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateSalesReturnResult { Id = salesReturn.Id, Number = salesReturn.Number };
    }
}
