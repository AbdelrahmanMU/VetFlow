namespace VetFlow.Application.Inventory;

/// <summary>
/// The Inventory module's public <b>sales-return</b> contract (BR-INV-069, DEC-INV-019) — the
/// operation committing a sales return depends on (REQ-SAL-004, BR-SAL-017/018). It is the inbound
/// counterpart of <see cref="IInventoryConsumptionWriter"/>, and it exists for the same reason:
/// <b>Sales expresses intent; Inventory performs execution</b> (BR-SAL-013, DEC-SAL-006).
///
/// <para>The caller states "put sale line L's portion (P, P+N] back", and receives success or a
/// rejection. It never names a batch, never sees one, and never learns which batches the quantity
/// went back to — which is why <see cref="VetFlow.Domain.Sales.SalesReturnLine"/> carries no
/// <c>BatchId</c> at all, unlike its Purchasing counterpart.</para>
///
/// <para>Inventory reads the <b>recorded consumption trace</b> of that sale line (REQ-INV-008,
/// BR-INV-057) — the <c>Consume</c> movements written when the sale committed — and returns the
/// quantity to exactly those batches, in the order they were consumed, taking each batch's share
/// before moving to the next (BR-SAL-017). <b>FEFO plays no part</b> and no batch is ever selected
/// (BR-INV-069): a return cannot invent a batch the goods never left. <b>Expired batches are not
/// excluded</b> either — the quantity goes back where it actually came from, and the ban on
/// <i>selling</i> expired stock is untouched (DEC-INV-021, BR-SAL-018, TS-SAL-022).</para>
///
/// <para><b>It applies and saves.</b> Unlike the consumption contract, which stages onto the
/// caller's unit of work, this one performs the single <c>SaveChanges</c> itself, through the
/// shared <c>BatchOperationWriter</c> that every Epic 2 batch operation goes through. The caller
/// must therefore have already staged its own document changes — the Draft → Committed transition —
/// <b>before</b> calling, so that one transaction covers the status, every batch, the on-hand
/// quantities and the ledger rows together (BR-SAL-018, BR-INV-062). That ordering is what
/// AC-SAL-019 asserts, and it is the C5 arrangement unchanged.</para>
///
/// <para>Rejections leave nothing behind: a batch that would exceed nothing but no longer exists
/// (404-shaped), a concurrent change to a batch (VTF-INV-068), or a consumption trace that cannot
/// support the return (VTF-SAL-020) all fail with nothing saved.</para>
/// </summary>
public interface IInventorySalesReturnWriter
{
    Task ApplyAsync(
        IReadOnlyCollection<InventorySalesReturnRequest> requests,
        CancellationToken cancellationToken);
}
