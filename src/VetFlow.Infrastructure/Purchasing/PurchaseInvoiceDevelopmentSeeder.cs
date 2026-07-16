using Microsoft.EntityFrameworkCore;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// Seeds a small set of sample purchase invoices for local browser verification
/// of the Slice-1 list — search, sort, filters, pagination (DEC-PUR-001 allows
/// 2–5 rows in the development database only). Idempotent: it does nothing once
/// any invoice exists. Numbers come from the same PostgreSQL sequence the future
/// create command will use (BR-PUR-002); timestamps come from
/// <see cref="TimeProvider"/> (STD-BE-045).
///
/// Received and cancelled invoices are written directly as persisted states —
/// the received/cancelled transitions belong to later slices, so no domain
/// method produces them yet (the same approach as Catalog's disabled-state seed).
/// </summary>
public static class PurchaseInvoiceDevelopmentSeeder
{
    public static async Task SeedAsync(
        VetFlowDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.PurchaseInvoices.AnyAsync(cancellationToken))
        {
            return;
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var createdAt = timeProvider.GetUtcNow();

        // Two invoices share an invoice date (2 days ago) so the default
        // newest-first order and stable pagination are visible even under ties.
        var drafts = new[]
        {
            await NewInvoiceAsync(dbContext, "شركة الدلتا للأدوية البيطرية", today.AddDays(-1), 4250.00m, createdAt, "INV-2024-0091", cancellationToken),
            await NewInvoiceAsync(dbContext, "مؤسسة النور للمستلزمات الطبية", today.AddDays(-2), 1875.50m, createdAt, supplierReference: null, cancellationToken),
            await NewInvoiceAsync(dbContext, "شركة الحياة لأعلاف الحيوان", today.AddDays(-2), 9600.00m, createdAt, "A-5567", cancellationToken),
            await NewInvoiceAsync(dbContext, "الشركة المصرية للقاحات", today.AddDays(-9), 3120.75m, createdAt, "EG-778", cancellationToken),
            await NewInvoiceAsync(dbContext, "مخازن الرحمة البيطرية", today.AddDays(-15), 640.00m, createdAt, supplierReference: null, cancellationToken),
        };

        // Vary the status across the sample so the badge and the status filter can
        // be exercised: keep two drafts, one received, one cancelled, one draft.
        SetStatus(dbContext, drafts[2], PurchaseInvoiceStatus.Received);
        SetStatus(dbContext, drafts[3], PurchaseInvoiceStatus.Cancelled);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<PurchaseInvoice> NewInvoiceAsync(
        VetFlowDbContext dbContext,
        string supplierName,
        DateOnly invoiceDate,
        decimal total,
        DateTimeOffset createdAt,
        string? supplierReference,
        CancellationToken cancellationToken)
    {
        var sequenceValue = await dbContext.Database
            .SqlQueryRaw<long>($"SELECT nextval('{InternalPurchaseInvoiceNumber.SequenceName}') AS \"Value\"")
            .SingleAsync(cancellationToken);

        var invoice = new PurchaseInvoice(
            Guid.NewGuid(),
            InternalPurchaseInvoiceNumber.Format(sequenceValue),
            supplierName,
            invoiceDate,
            total,
            createdAt,
            supplierReference);

        dbContext.PurchaseInvoices.Add(invoice);
        return invoice;
    }

    private static void SetStatus(VetFlowDbContext dbContext, PurchaseInvoice invoice, PurchaseInvoiceStatus status) =>
        dbContext.Entry(invoice).Property(entity => entity.Status).CurrentValue = status;
}
