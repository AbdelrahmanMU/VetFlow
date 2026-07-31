using Microsoft.EntityFrameworkCore;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Sales;

/// <summary>
/// The one implementation of BR-SAL-016's returnable ceiling, shared by the add-line write path, the
/// read path behind the return screen, and the commit path that tells Inventory where a return
/// resumes. It exists so the rule is computed once: two implementations would drift, and the screen
/// would eventually promise a quantity the command rejects.
///
/// <para><b>Derived, never stored.</b> "How much of this line has already been returned" is the sum
/// of the lines of its <b>committed</b> returns — it is not a column on
/// <see cref="SalesLineItem"/>. A stored counter would be a second source of truth that can
/// disagree with the documents that produced it, and would need a backfill; a sum cannot drift from
/// its own inputs.</para>
///
/// <para><b>Only committed returns count</b> (BR-SAL-016). Drafts do not reserve, which is a
/// deliberate choice with a visible consequence: two drafts can each pass validation and the second
/// fails at commit. The alternative — reservation — is a whole mechanism nobody ruled, and
/// reservations are themselves out of scope (DEC-SAL-001).</para>
/// </summary>
internal static class SalesReturnableQuantities
{
    /// <summary>
    /// Quantity already returned per original sale line, across every <b>committed</b> return of the
    /// given invoice. Lines with no returns are absent from the dictionary (⇒ zero).
    /// </summary>
    public static Task<Dictionary<Guid, decimal>> GetAlreadyReturnedAsync(
        VetFlowDbContext dbContext,
        Guid salesInvoiceId,
        CancellationToken cancellationToken)
    {
        // Queried through the line DbSet and the shadow FK rather than by walking the aggregate's
        // Lines navigation: that collection is encapsulated (it exposes AsReadOnly over a private
        // field), which is right for the domain but is not something the provider can translate into
        // a SQL join. This shape is one grouped SELECT.
        var committedReturnIds = dbContext.SalesReturns
            .Where(item => item.SalesInvoiceId == salesInvoiceId
                && item.Status == SalesReturnStatus.Committed)
            .Select(item => item.Id);

        return dbContext.SalesReturnLines
            .Where(line => committedReturnIds.Contains(EF.Property<Guid>(line, "SalesReturnId")))
            .GroupBy(line => line.SalesLineItemId)
            .Select(group => new { SalesLineItemId = group.Key, Returned = group.Sum(line => line.Quantity) })
            .ToDictionaryAsync(row => row.SalesLineItemId, row => row.Returned, cancellationToken);
    }

    /// <summary>
    /// The same sums, restricted to the committed returns of one sale line — what the commit path
    /// needs to tell Inventory <b>where a return resumes</b> (BR-SAL-017). It excludes the return
    /// being committed, which is exactly right: that document's own quantity is what is being
    /// applied now, and counting it would skip its own share of the trace.
    /// </summary>
    public static Task<decimal> GetAlreadyReturnedForLineAsync(
        VetFlowDbContext dbContext,
        Guid salesLineItemId,
        Guid excludedReturnId,
        CancellationToken cancellationToken)
    {
        var committedReturnIds = dbContext.SalesReturns
            .Where(item => item.Status == SalesReturnStatus.Committed && item.Id != excludedReturnId)
            .Select(item => item.Id);

        return dbContext.SalesReturnLines
            .Where(line => line.SalesLineItemId == salesLineItemId
                && committedReturnIds.Contains(EF.Property<Guid>(line, "SalesReturnId")))
            .SumAsync(line => line.Quantity, cancellationToken);
    }
}
