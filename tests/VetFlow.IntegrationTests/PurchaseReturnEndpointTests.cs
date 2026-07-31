using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VetFlow.Domain.Inventory;
using VetFlow.Domain.Purchasing;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Purchase returns (REQ-PUR-006, DEC-PUR-010) end to end through the real API and a real
/// PostgreSQL. Covers TS-PUR-034..041.
///
/// <para>Every batch these tests return against is created by the <b>real receiving path</b>
/// (create invoice → add line → receive), never by inserting a batch row. A return is only
/// meaningful against stock that actually arrived, so seeding the batch directly would prove the
/// return works against a fixture rather than against the system.</para>
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class PurchaseReturnEndpointTests(ApiFixture fixture)
{
    /// <summary>
    /// The seeded product's unit chain is carton → 12 boxes → 10 strips, so one purchase carton is
    /// <b>120 stock units</b> (BR-INV-058: batches hold the smallest measurable unit).
    ///
    /// <para>This constant is the point of several assertions below: a return line's quantity is in
    /// the <b>original line's purchase unit</b> — that is what BR-PUR-016 caps and what the screen
    /// shows — while the stock it moves is in stock units. Returning 4 cartons must remove 480, not
    /// 4. Asserting the converted number is what makes that visible.</para>
    /// </summary>
    private const decimal StockUnitsPerCarton = 120m;

    [Fact]
    public async Task A_draft_return_is_created_against_a_received_invoice_TS_PUR_034()
    {
        var seed = await ReceivedInvoiceAsync(quantity: 10m);

        var response = await CreateReturnAsync(seed.InvoiceId);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("number").GetString().ShouldStartWith("PRT-");

        var returnId = body.RootElement.GetProperty("id").GetGuid();
        var stored = await ReturnAsync(returnId);
        stored.Status.ShouldBe(PurchaseReturnStatus.Draft);
        stored.PurchaseInvoiceId.ShouldBe(seed.InvoiceId);
        stored.SupplierName.ShouldBe(seed.SupplierName);   // snapshot from the original invoice
    }

    [Fact]
    public async Task A_return_against_a_draft_invoice_is_rejected_TS_PUR_035()
    {
        // A draft invoice never put anything into stock, so returning against it is meaningless
        // (BR-PUR-015) — and it is rejected here, early and legibly, rather than later at the floor.
        var seed = await SeedInvoiceAsync();
        await AddLineAsync(seed.InvoiceId, seed.ProductId, 5m);

        var rejected = await CreateReturnAsync(seed.InvoiceId);

        rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(rejected)).ShouldBe(PurchasingErrorCodes.ReturnOriginalInvoiceNotReceived);
    }

    [Fact]
    public async Task A_partial_return_is_accepted_and_lowers_the_remaining_returnable_TS_PUR_036()
    {
        var seed = await ReceivedInvoiceAsync(quantity: 10m);
        var returnId = await NewReturnAsync(seed.InvoiceId);

        (await AddReturnLineAsync(returnId, seed.LineId, 3m)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await CommitAsync(returnId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var returnable = await ReturnableAsync(seed.InvoiceId);
        returnable.Single().GetProperty("returnableQuantity").GetDecimal().ShouldBe(7m);
        returnable.Single().GetProperty("returnedQuantity").GetDecimal().ShouldBe(3m);
    }

    [Fact]
    public async Task Returning_more_than_remains_is_rejected_with_no_effect_TS_PUR_037()
    {
        var seed = await ReceivedInvoiceAsync(quantity: 10m);
        var first = await NewReturnAsync(seed.InvoiceId);
        await AddReturnLineAsync(first, seed.LineId, 7m);
        await CommitAsync(first);

        var second = await NewReturnAsync(seed.InvoiceId);
        var rejected = await AddReturnLineAsync(second, seed.LineId, 4m);   // only 3 remain

        rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(rejected)).ShouldBe(PurchasingErrorCodes.ReturnQuantityExceedsReturnable);

        // Nothing moved on the rejection: the first return's 7 is the whole effect.
        (await RemainingAsync(seed.BatchId)).ShouldBe(3m * StockUnitsPerCarton);
        (await OnHandAsync(seed.ProductId)).ShouldBe(3m * StockUnitsPerCarton);
        (await MovementsAsync(seed.BatchId, InventoryMovementType.PurchaseReturn)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_draft_return_does_not_reserve_quantity_TS_PUR_038()
    {
        // The deliberate consequence of "only committed returns count" (BR-PUR-016): a draft holds
        // nothing back, so a second document may still take the full remainder.
        var seed = await ReceivedInvoiceAsync(quantity: 10m);

        var draft = await NewReturnAsync(seed.InvoiceId);
        (await AddReturnLineAsync(draft, seed.LineId, 5m)).StatusCode.ShouldBe(HttpStatusCode.Created);

        var other = await NewReturnAsync(seed.InvoiceId);
        (await AddReturnLineAsync(other, seed.LineId, 10m)).StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Committing_moves_both_quantities_and_writes_one_ledger_row_TS_PUR_039()
    {
        var seed = await ReceivedInvoiceAsync(quantity: 10m);
        var returnId = await NewReturnAsync(seed.InvoiceId);
        var lineId = await AddReturnLineIdAsync(returnId, seed.LineId, 4m);

        (await CommitAsync(returnId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // BR-INV-005 holds: the batch remainder and the product on-hand moved by the same amount.
        (await RemainingAsync(seed.BatchId)).ShouldBe(6m * StockUnitsPerCarton);
        (await OnHandAsync(seed.ProductId)).ShouldBe(6m * StockUnitsPerCarton);
        (await ReceivedQuantityAsync(seed.BatchId)).ShouldBe(10m * StockUnitsPerCarton);   // history never changes

        var movement = (await MovementsAsync(seed.BatchId, InventoryMovementType.PurchaseReturn)).Single();
        movement.Quantity.ShouldBe(-4m * StockUnitsPerCarton);   // 4 cartons = 480 stock units
        movement.Source.ShouldBe(InventoryMovementSource.Purchasing);
        movement.ReferenceId.ShouldBe(lineId);      // traceable to its return line (BR-INV-057)
        movement.Reason.ShouldBeNull();             // returns carry no reason (BR-INV-067)

        (await ReturnAsync(returnId)).Status.ShouldBe(PurchaseReturnStatus.Committed);
    }

    [Fact]
    public async Task A_return_leaves_its_own_batch_even_when_another_expires_sooner_TS_PUR_040()
    {
        // The FEFO trap: a second batch of the same product expires sooner. A return is bound to
        // its origin (BR-PUR-017, BR-INV-069), so the nearer-expiry batch must not be touched.
        var seed = await ReceivedInvoiceAsync(quantity: 10m, expiry: fixture.ClinicToday.AddDays(90));
        var sooner = await ReceiveAnotherBatchAsync(seed.ProductId, quantity: 10m, expiry: fixture.ClinicToday.AddDays(5));

        var returnId = await NewReturnAsync(seed.InvoiceId);
        await AddReturnLineAsync(returnId, seed.LineId, 4m);
        await CommitAsync(returnId);

        (await RemainingAsync(seed.BatchId)).ShouldBe(6m * StockUnitsPerCarton);      // the origin batch paid
        (await RemainingAsync(sooner.BatchId)).ShouldBe(10m * StockUnitsPerCarton);   // the nearer-expiry one is untouched
    }

    [Fact]
    public async Task A_committed_return_rejects_every_further_change_TS_PUR_041()
    {
        var seed = await ReceivedInvoiceAsync(quantity: 10m);
        var returnId = await NewReturnAsync(seed.InvoiceId);
        var lineId = await AddReturnLineIdAsync(returnId, seed.LineId, 2m);
        await CommitAsync(returnId);

        (await AddReturnLineAsync(returnId, seed.LineId, 1m)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CommitAsync(returnId)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var removal = await fixture.Client.DeleteAsync(
            new Uri($"/api/v1/purchase-returns/{returnId}/lines/{lineId}", UriKind.Relative));
        removal.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // One movement only — none of the rejected calls moved anything.
        (await MovementsAsync(seed.BatchId, InventoryMovementType.PurchaseReturn)).Count.ShouldBe(1);
        (await RemainingAsync(seed.BatchId)).ShouldBe(8m * StockUnitsPerCarton);
    }

    [Fact]
    public async Task An_empty_return_cannot_be_committed_BR_PUR_018()
    {
        var seed = await ReceivedInvoiceAsync(quantity: 5m);
        var returnId = await NewReturnAsync(seed.InvoiceId);

        var rejected = await CommitAsync(returnId);

        rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(rejected)).ShouldBe(PurchasingErrorCodes.ReturnHasNoLines);
    }

    [Fact]
    public async Task The_returnable_read_is_404_for_an_invoice_that_cannot_be_returned_BR_PUR_015()
    {
        var draft = await SeedInvoiceAsync();
        await AddLineAsync(draft.InvoiceId, draft.ProductId, 5m);

        var response = await fixture.Client.GetAsync(ReturnableUri(draft.InvoiceId));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---- seeding and helpers ----------------------------------------------------------------------

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];

    private async Task<(Guid InvoiceId, Guid ProductId, string SupplierName)> SeedInvoiceAsync()
    {
        var marker = Marker();
        var supplierName = $"مورد {marker}";
        Guid invoiceId = Guid.Empty;
        Guid productId = Guid.Empty;

        await fixture.SeedAsync(async dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            var product = CatalogSeeder.NewProduct(
                dbContext, $"منتج {marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature,
                hasExpiration: true);
            var invoice = await PurchasingSeeder.NewInvoiceAsync(
                dbContext, supplierName, new DateOnly(2026, 5, 1), 0m);
            productId = product.Id;
            invoiceId = invoice.Id;
        });

        return (invoiceId, productId, supplierName);
    }

    private async Task<(Guid InvoiceId, Guid ProductId, Guid LineId, Guid BatchId, string SupplierName)>
        ReceivedInvoiceAsync(decimal quantity, DateOnly? expiry = null)
    {
        var seed = await SeedInvoiceAsync();
        var lineId = await AddLineAsync(seed.InvoiceId, seed.ProductId, quantity);

        var receive = await fixture.Client.PostAsJsonAsync(
            new Uri($"/api/v1/purchase-invoices/{seed.InvoiceId}/receive", UriKind.Relative),
            new { lines = new[] { new { lineId, expiryDate = expiry ?? fixture.ClinicToday.AddDays(180) } } });
        receive.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var batchId = await fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.PurchaseLineId == lineId).Select(batch => batch.Id).SingleAsync());

        return (seed.InvoiceId, seed.ProductId, lineId, batchId, seed.SupplierName);
    }

    private async Task<(Guid BatchId, Guid LineId)> ReceiveAnotherBatchAsync(
        Guid productId, decimal quantity, DateOnly expiry)
    {
        var marker = Marker();
        Guid invoiceId = Guid.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var invoice = await PurchasingSeeder.NewInvoiceAsync(
                dbContext, $"مورد {marker}", new DateOnly(2026, 5, 2), 0m);
            invoiceId = invoice.Id;
        });

        var lineId = await AddLineAsync(invoiceId, productId, quantity);
        var receive = await fixture.Client.PostAsJsonAsync(
            new Uri($"/api/v1/purchase-invoices/{invoiceId}/receive", UriKind.Relative),
            new { lines = new[] { new { lineId, expiryDate = expiry } } });
        receive.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var batchId = await fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.PurchaseLineId == lineId).Select(batch => batch.Id).SingleAsync());

        return (batchId, lineId);
    }

    private async Task<Guid> AddLineAsync(Guid invoiceId, Guid productId, decimal quantity)
    {
        var response = await fixture.Client.PostAsJsonAsync(
            new Uri($"/api/v1/purchase-invoices/{invoiceId}/lines", UriKind.Relative),
            new { productId, purchaseUnitId = SeededCatalogIds.CartonUnit, quantity, unitPrice = 100m });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("lineId").GetGuid();
    }

    private Task<HttpResponseMessage> CreateReturnAsync(Guid invoiceId) =>
        fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/purchase-returns", UriKind.Relative),
            new { purchaseInvoiceId = invoiceId, returnDate = fixture.ClinicToday, notes = (string?)null });

    private async Task<Guid> NewReturnAsync(Guid invoiceId)
    {
        var response = await CreateReturnAsync(invoiceId);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> AddReturnLineAsync(Guid returnId, Guid purchaseLineItemId, decimal quantity) =>
        fixture.Client.PostAsJsonAsync(
            new Uri($"/api/v1/purchase-returns/{returnId}/lines", UriKind.Relative),
            new { purchaseLineItemId, quantity });

    private async Task<Guid> AddReturnLineIdAsync(Guid returnId, Guid purchaseLineItemId, decimal quantity)
    {
        var response = await AddReturnLineAsync(returnId, purchaseLineItemId, quantity);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> CommitAsync(Guid returnId) =>
        fixture.Client.PostAsync(
            new Uri($"/api/v1/purchase-returns/{returnId}/commit", UriKind.Relative), content: null);

    private async Task<JsonElement[]> ReturnableAsync(Guid invoiceId)
    {
        var response = await fixture.Client.GetAsync(ReturnableUri(invoiceId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. body.RootElement.EnumerateArray().Select(element => element.Clone())];
    }

    private static Uri ReturnableUri(Guid invoiceId) =>
        new($"/api/v1/purchase-invoices/{invoiceId}/returnable-lines", UriKind.Relative);

    private Task<PurchaseReturn> ReturnAsync(Guid returnId) =>
        fixture.QueryDbAsync(dbContext => dbContext.PurchaseReturns
            .Include(item => item.Lines)
            .SingleAsync(item => item.Id == returnId));

    private Task<decimal> RemainingAsync(Guid batchId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.Id == batchId).Select(batch => batch.RemainingQuantity).SingleAsync());

    private Task<decimal> ReceivedQuantityAsync(Guid batchId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.Id == batchId).Select(batch => batch.Quantity).SingleAsync());

    private Task<decimal> OnHandAsync(Guid productId) =>
        fixture.QueryDbAsync(dbContext => dbContext.ProductOnHands
            .Where(item => item.ProductId == productId).Select(item => item.OnHandQuantity).SingleAsync());

    private Task<List<InventoryMovement>> MovementsAsync(Guid batchId, InventoryMovementType type) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryMovements
            .Where(movement => movement.BatchId == batchId && movement.Type == type).ToListAsync());

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("errorCode").GetString();
    }
}
