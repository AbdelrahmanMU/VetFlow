using Microsoft.EntityFrameworkCore;
using VetFlow.Domain.Purchasing;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.Infrastructure.Purchasing;

/// <summary>
/// The one implementation of BR-PUR-016's returnable ceiling, shared by the add-line write path and
/// the read path behind the return screen. It exists so the rule is computed once: two
/// implementations would drift, and the screen would eventually promise a quantity the command
/// rejects.
///
/// <para><b>Derived, never stored.</b> "How much of this line has already been returned" is the sum
/// of the lines of its <b>committed</b> returns — it is not a column on
/// <see cref="PurchaseLineItem"/>. A stored counter would be a second source of truth that can
/// disagree with the documents that produced it, and would need a backfill; a sum cannot drift from
/// its own inputs.</para>
///
/// <para><b>Only committed returns count</b> (BR-PUR-016). Drafts do not reserve, which is a
/// deliberate choice with a visible consequence: two drafts can each pass validation and the second
/// fails at commit. The alternative — reservation — is a whole mechanism nobody ruled, and
/// reservations are themselves out of scope (DEC-SAL-001).</para>
/// </summary>
internal static class PurchaseReturnableQuantities
{
    /// <summary>
    /// Quantity already returned per original purchase line, across every <b>committed</b> return
    /// of the given invoice. Lines with no returns are absent from the dictionary (⇒ zero).
    /// </summary>
    public static async Task<Dictionary<Guid, decimal>> GetAlreadyReturnedAsync(
        VetFlowDbContext dbContext,
        Guid purchaseInvoiceId,
        CancellationToken cancellationToken)
    {
        // Queried through the line DbSet and the shadow FK rather than by walking the aggregate's
        // Lines navigation: that collection is encapsulated (it exposes AsReadOnly over a private
        // field), which is right for the domain but is not something the provider can translate
        // into a SQL join. This shape is one grouped SELECT.
        var committedReturnIds = dbContext.PurchaseReturns
            .Where(item => item.PurchaseInvoiceId == purchaseInvoiceId
                && item.Status == PurchaseReturnStatus.Committed)
            .Select(item => item.Id);

        return await dbContext.PurchaseReturnLines
            .Where(line => committedReturnIds.Contains(EF.Property<Guid>(line, "PurchaseReturnId")))
            .GroupBy(line => line.PurchaseLineItemId)
            .Select(group => new { PurchaseLineItemId = group.Key, Returned = group.Sum(line => line.Quantity) })
            .ToDictionaryAsync(row => row.PurchaseLineItemId, row => row.Returned, cancellationToken);
    }
}
