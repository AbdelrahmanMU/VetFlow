using VetFlow.Application.Common;
using VetFlow.Application.Purchasing.Commands.CreatePurchaseInvoice;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;
using VetFlow.Infrastructure.Persistence.Numbering;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Create-purchase-invoice write path (REQ-PUR-003) — Purchasing's first command.
/// It allocates the number from its branch's counter (BR-PUR-002 — unique and
/// ascending under concurrency; ADR-0022 §6), builds the aggregate through the domain
/// constructor (born a draft with a zero total, every BR-PUR invariant enforced
/// there — STD-BE-010), and persists it in a single <c>SaveChanges</c>
/// (STD-BE-024). Mirrors the Catalog create-product handler; no line items, no
/// total derivation, no inventory effect (scope lock, DEC-PUR-001).
/// </summary>
public sealed class CreatePurchaseInvoiceCommandHandler(
    VetFlowDbContext dbContext,
    DocumentNumbers documentNumbers,
    TimeProvider timeProvider)
    : ICommandHandler<CreatePurchaseInvoiceCommand, CreatePurchaseInvoiceResult>
{
    public async Task<CreatePurchaseInvoiceResult> HandleAsync(
        CreatePurchaseInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        // One transaction around the allocation and the insert, so a failed save gives the number
        // back instead of burning it (ADR-0022 §6 — numbering is gapless by owner ruling).
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var number = InternalPurchaseInvoiceNumber.Format(
            await documentNumbers.NextAsync(DocumentSeries.PurchaseInvoice, cancellationToken));

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
        await transaction.CommitAsync(cancellationToken);

        return new CreatePurchaseInvoiceResult { Id = invoice.Id, Number = invoice.Number };
    }
}
