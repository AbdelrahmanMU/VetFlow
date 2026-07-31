using Microsoft.EntityFrameworkCore;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;
using VetFlow.Infrastructure.Sales;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Builds sales invoices through the domain constructor — never raw rows —
/// allocating the real <c>SAL-</c> number from the same PostgreSQL sequence the
/// production path uses (BR-SAL-002). The total and the committed state are
/// written directly for list scenarios (the total is line-derived and commit
/// needs stock — both belong to other tests; the PurchasingSeeder approach).
/// </summary>
public static class SalesSeeder
{
    private static readonly DateTimeOffset DefaultCreatedAt = new(2026, 7, 1, 9, 0, 0, TimeSpan.Zero);

    public static async Task<SalesInvoice> NewInvoiceAsync(
        VetFlowDbContext dbContext,
        string? customerName,
        DateOnly saleDate,
        decimal total,
        string? notes = null,
        DateTimeOffset? createdAt = null)
    {
        var sequenceValue = await dbContext.Database
            .SqlQueryRaw<long>($"SELECT nextval('{InternalSalesInvoiceNumber.SequenceName}') AS \"Value\"")
            .SingleAsync();

        var invoice = new SalesInvoice(
            Guid.NewGuid(),
            InternalSalesInvoiceNumber.Format(sequenceValue),
            saleDate,
            createdAt ?? DefaultCreatedAt,
            customerName,
            notes);

        dbContext.SalesInvoices.Add(invoice);
        dbContext.Entry(invoice).Property(entity => entity.TotalAmount).CurrentValue = total;
        return invoice;
    }

    public static void SetStatus(VetFlowDbContext dbContext, SalesInvoice invoice, SalesInvoiceStatus status) =>
        dbContext.Entry(invoice).Property(entity => entity.Status).CurrentValue = status;
}
