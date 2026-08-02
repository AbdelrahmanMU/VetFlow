using Microsoft.EntityFrameworkCore;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;
using VetFlow.Infrastructure.Persistence.Numbering;
using VetFlow.Infrastructure.Purchasing;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Builds purchase invoices through the domain constructor — never raw rows —
/// allocating the real <c>PUR-</c> number from the same branch counter the
/// production path uses (BR-PUR-002, ADR-0022 §6). Received/cancelled states are written
/// directly (the transitions belong to later slices — the same approach as
/// Catalog's disabled-state seed).
/// </summary>
public static class PurchasingSeeder
{
    private static readonly DateTimeOffset DefaultCreatedAt = new(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<PurchaseInvoice> NewInvoiceAsync(
        VetFlowDbContext dbContext,
        string supplierName,
        DateOnly invoiceDate,
        decimal total,
        string? supplierReference = null,
        string? notes = null,
        DateTimeOffset? createdAt = null)
    {
        var sequenceValue = await TestDocumentNumbers.NextAsync(dbContext, DocumentSeries.PurchaseInvoice);

        var invoice = new PurchaseInvoice(
            Guid.NewGuid(),
            InternalPurchaseInvoiceNumber.Format(sequenceValue),
            supplierName,
            invoiceDate,
            total,
            createdAt ?? DefaultCreatedAt,
            supplierReference,
            notes);

        dbContext.PurchaseInvoices.Add(invoice);
        return invoice;
    }

    public static void SetStatus(VetFlowDbContext dbContext, PurchaseInvoice invoice, PurchaseInvoiceStatus status) =>
        dbContext.Entry(invoice).Property(entity => entity.Status).CurrentValue = status;
}
