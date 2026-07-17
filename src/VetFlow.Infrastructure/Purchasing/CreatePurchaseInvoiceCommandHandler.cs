using Microsoft.EntityFrameworkCore;
using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Commands.CreatePurchaseInvoice;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Create-purchase-invoice write path (REQ-PUR-003) — Purchasing's first command.
/// It allocates the number from the PostgreSQL sequence (BR-PUR-002 — unique and
/// ascending under concurrency), builds the aggregate through the domain
/// constructor (born a draft with a zero total, every BR-PUR invariant enforced
/// there — STD-BE-010), and persists it in a single <c>SaveChanges</c>
/// (STD-BE-024). Mirrors the Catalog create-product handler; no line items, no
/// total derivation, no inventory effect (scope lock, DEC-PUR-001).
/// </summary>
public sealed class CreatePurchaseInvoiceCommandHandler(VetFlowDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<CreatePurchaseInvoiceCommand, CreatePurchaseInvoiceResult>
{
    public async Task<CreatePurchaseInvoiceResult> HandleAsync(
        CreatePurchaseInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        var sequenceValue = await dbContext.Database
            .SqlQueryRaw<long>($"SELECT nextval('{InternalPurchaseInvoiceNumber.SequenceName}') AS \"Value\"")
            .SingleAsync(cancellationToken);
        var number = InternalPurchaseInvoiceNumber.Format(sequenceValue);

        // The validator guarantees InvoiceDate is present before the handler runs.
        var invoice = new PurchaseInvoice(
            Guid.NewGuid(),
            number,
            command.SupplierName,
            command.InvoiceDate!.Value,
            totalAmount: 0m,
            timeProvider.GetUtcNow(),
            command.SupplierInvoiceReference,
            command.Notes);

        dbContext.PurchaseInvoices.Add(invoice);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreatePurchaseInvoiceResult { Id = invoice.Id, Number = invoice.Number };
    }
}
