using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VetFlow.Domain.Inventory;
using VetFlow.Domain.Sales;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Sales returns (REQ-SAL-004, DEC-SAL-010) end to end through the real API and a real PostgreSQL.
/// Covers TS-SAL-016..023.
///
/// <para>Every batch these tests return into was created by the <b>real receiving path</b> and
/// emptied by the <b>real sale path</b> (create → add line → commit), never by inserting rows. The
/// whole point of C6 is that a return goes back to the batches the goods actually left, and that
/// route only exists if the consumption trace was written by the code that consumes.</para>
///
/// <para><b>The unit chain matters here.</b> The seeded product is carton → 12 boxes → 10 strips, so
/// the stock unit is the strip: one carton is <b>120 strips</b> and one box is <b>10</b>. Sales are
/// made in boxes, so a return line's quantity is in boxes while the stock it moves is in strips —
/// and the factor is derived from what the sale actually consumed, not from the catalog.</para>
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SalesReturnEndpointTests(ApiFixture fixture)
{
    private const decimal StripsPerBox = 10m;
    private const decimal StripsPerCarton = 120m;

    [Fact]
    public async Task A_draft_return_is_created_against_a_committed_invoice_TS_SAL_016()
    {
        var seed = await SoldAsync(cartons: 1m, boxes: 5m);

        var response = await CreateReturnAsync(seed.InvoiceId);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("number").GetString().ShouldStartWith("SRT-");

        var returnId = body.RootElement.GetProperty("id").GetGuid();
        var stored = await ReturnAsync(returnId);
        stored.Status.ShouldBe(SalesReturnStatus.Draft);
        stored.SalesInvoiceId.ShouldBe(seed.InvoiceId);
        stored.CustomerName.ShouldBe(seed.CustomerName);   // snapshot from the original invoice
    }

    [Fact]
    public async Task A_return_against_a_draft_invoice_is_rejected_TS_SAL_017()
    {
        // A draft invoice never consumed stock, so there is no trace to return along (BR-SAL-015).
        // Rejected early and legibly rather than failing later with nowhere to put the quantity.
        var seed = await ReceivedAsync(cartons: 1m);
        var invoiceId = await CreateInvoiceAsync();
        await AddSaleLineAsync(invoiceId, seed.ProductId, 2m);

        var rejected = await CreateReturnAsync(invoiceId);

        rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(rejected)).ShouldBe(SalesErrorCodes.ReturnOriginalInvoiceNotCommitted);

        // And the screen behind it is 404 too — a table of lines is never rendered for an invoice
        // whose return the command would reject.
        (await fixture.Client.GetAsync(ReturnableUri(invoiceId))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_partial_return_is_accepted_and_lowers_the_remaining_returnable_TS_SAL_018()
    {
        var seed = await SoldAsync(cartons: 1m, boxes: 10m);
        var returnId = await NewReturnAsync(seed.InvoiceId);

        (await AddReturnLineAsync(returnId, seed.SaleLineId, 3m)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await CommitAsync(returnId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var returnable = (await ReturnableAsync(seed.InvoiceId)).Single();
        returnable.GetProperty("returnableQuantity").GetDecimal().ShouldBe(7m);
        returnable.GetProperty("returnedQuantity").GetDecimal().ShouldBe(3m);
        returnable.GetProperty("quantity").GetDecimal().ShouldBe(10m);

        // 3 boxes came back as 30 strips — the return line is in the sale unit, the stock in the
        // stock unit (BR-INV-058).
        (await RemainingAsync(seed.BatchId)).ShouldBe(StripsPerCarton - (10m * StripsPerBox) + (3m * StripsPerBox));
    }

    [Fact]
    public async Task Returning_more_than_remains_is_rejected_with_no_effect_TS_SAL_019()
    {
        var seed = await SoldAsync(cartons: 1m, boxes: 10m);
        var first = await NewReturnAsync(seed.InvoiceId);
        await AddReturnLineAsync(first, seed.SaleLineId, 7m);
        await CommitAsync(first);

        var remainingBefore = await RemainingAsync(seed.BatchId);
        var onHandBefore = await OnHandAsync(seed.ProductId);

        var second = await NewReturnAsync(seed.InvoiceId);
        var rejected = await AddReturnLineAsync(second, seed.SaleLineId, 4m);   // only 3 remain

        rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(rejected)).ShouldBe(SalesErrorCodes.ReturnQuantityExceedsReturnable);

        // Nothing moved on the rejection, and no ledger row was written.
        (await RemainingAsync(seed.BatchId)).ShouldBe(remainingBefore);
        (await OnHandAsync(seed.ProductId)).ShouldBe(onHandBefore);
        (await MovementsAsync(seed.BatchId, InventoryMovementType.SalesReturn)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_split_sale_line_returns_to_its_batches_in_consumption_order_TS_SAL_020()
    {
        // The rule C6 exists for (BR-SAL-017, AC-SAL-018). FEFO split one sale line across two
        // batches; a partial return must go back to the batches the goods actually left, in the
        // order they left, and never by FEFO or any other selection.
        var seed = await SoldAcrossTwoBatchesAsync();

        // The sale of 15 boxes (150 strips) took all 120 from the nearer-expiry batch A and 30 from
        // batch B. Returning 13 boxes (130 strips) fills A first, then puts the rest into B.
        var returnId = await NewReturnAsync(seed.InvoiceId);
        var lineId = await AddReturnLineIdAsync(returnId, seed.SaleLineId, 13m);
        (await CommitAsync(returnId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await RemainingAsync(seed.BatchA)).ShouldBe(120m);          // 0 + 120
        (await RemainingAsync(seed.BatchB)).ShouldBe(90m + 10m);     // 90 + 10

        // One ledger row per batch, both typed SalesReturn and both traceable to the return line.
        var toA = (await MovementsAsync(seed.BatchA, InventoryMovementType.SalesReturn)).Single();
        var toB = (await MovementsAsync(seed.BatchB, InventoryMovementType.SalesReturn)).Single();
        toA.Quantity.ShouldBe(120m);
        toB.Quantity.ShouldBe(10m);
        toA.Source.ShouldBe(InventoryMovementSource.Sales);
        toA.ReferenceId.ShouldBe(lineId);
        toB.ReferenceId.ShouldBe(lineId);
        toA.Reason.ShouldBeNull();       // returns carry no reason (BR-INV-067)

        // A second partial return resumes where the first stopped: batch A has already been made
        // whole, so the remaining 2 boxes go to B alone. Refilling A here would push it above what
        // it ever gave.
        var second = await NewReturnAsync(seed.InvoiceId);
        await AddReturnLineAsync(second, seed.SaleLineId, 2m);
        (await CommitAsync(second)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await RemainingAsync(seed.BatchA)).ShouldBe(120m);          // untouched by the second return
        (await RemainingAsync(seed.BatchB)).ShouldBe(90m + 30m);     // and now whole as well

        // Fully returned: both batches are back to exactly what they held before the sale.
        (await OnHandAsync(seed.ProductId)).ShouldBe(2m * StripsPerCarton);
    }

    [Fact]
    public async Task Committing_moves_the_batches_and_the_on_hand_together_TS_SAL_021()
    {
        var seed = await SoldAsync(cartons: 1m, boxes: 6m);
        var beforeRemaining = await RemainingAsync(seed.BatchId);
        var beforeOnHand = await OnHandAsync(seed.ProductId);

        var returnId = await NewReturnAsync(seed.InvoiceId);
        await AddReturnLineAsync(returnId, seed.SaleLineId, 4m);
        (await CommitAsync(returnId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // BR-INV-005: the batch remainder and the product on-hand moved by the same amount.
        var batchGain = await RemainingAsync(seed.BatchId) - beforeRemaining;
        var onHandGain = await OnHandAsync(seed.ProductId) - beforeOnHand;
        batchGain.ShouldBe(4m * StripsPerBox);
        onHandGain.ShouldBe(batchGain);

        // The received quantity is history and never changes.
        (await ReceivedQuantityAsync(seed.BatchId)).ShouldBe(StripsPerCarton);
        (await ReturnAsync(returnId)).Status.ShouldBe(SalesReturnStatus.Committed);
    }

    [Fact]
    public async Task A_return_into_an_expired_batch_is_allowed_TS_SAL_022()
    {
        // BR-SAL-018: the quantity goes back where it actually came from. Refusing an expired batch
        // would strand the stock with no home, while the ban on *selling* it is untouched
        // (DEC-INV-021) — it stays out of every future allocation.
        var seed = await SoldAsync(cartons: 1m, boxes: 5m, expiry: fixture.ClinicToday);

        // The clinic clock cannot be moved in an integration test and ExpiryDate is immutable in the
        // domain by design, so the day rolling over is simulated at the store — the one thing that
        // cannot be expressed through the API.
        await ExpireBatchAsync(seed.BatchId);

        var returnId = await NewReturnAsync(seed.InvoiceId);
        await AddReturnLineAsync(returnId, seed.SaleLineId, 5m);

        (await CommitAsync(returnId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await RemainingAsync(seed.BatchId)).ShouldBe(StripsPerCarton);   // fully restored
        (await MovementsAsync(seed.BatchId, InventoryMovementType.SalesReturn)).Single()
            .Quantity.ShouldBe(5m * StripsPerBox);

        // And it is still unsaleable: a new sale of the returned stock is rejected for lack of
        // saleable stock, because every batch of the product is expired.
        var newInvoice = await CreateInvoiceAsync();
        await AddSaleLineAsync(newInvoice, seed.ProductId, 1m);
        (await CommitSaleAsync(newInvoice)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_committed_return_rejects_every_further_change_and_has_no_financial_effect_TS_SAL_023()
    {
        var seed = await SoldAsync(cartons: 1m, boxes: 8m);
        var totalBefore = await InvoiceTotalAsync(seed.InvoiceId);

        var returnId = await NewReturnAsync(seed.InvoiceId);
        var lineId = await AddReturnLineIdAsync(returnId, seed.SaleLineId, 2m);
        await CommitAsync(returnId);

        (await AddReturnLineAsync(returnId, seed.SaleLineId, 1m)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CommitAsync(returnId)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var removal = await fixture.Client.DeleteAsync(
            new Uri($"/api/v1/sales-returns/{returnId}/lines/{lineId}", UriKind.Relative));
        removal.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        // There is no cancel route at all (DEC-INV-037) — not a rejected one, an absent one.
        var cancel = await fixture.Client.PostAsync(
            new Uri($"/api/v1/sales-returns/{returnId}/cancel", UriKind.Relative), content: null);
        cancel.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // One movement only — none of the rejected calls moved anything.
        (await MovementsAsync(seed.BatchId, InventoryMovementType.SalesReturn)).Count.ShouldBe(1);

        // No financial effect anywhere (DEC-INV-035): the original invoice's total is untouched, and
        // the return itself carries no amount to report.
        (await InvoiceTotalAsync(seed.InvoiceId)).ShouldBe(totalBefore);
    }

    [Fact]
    public async Task Two_drafts_can_both_pass_and_the_second_fails_at_commit_BR_SAL_016()
    {
        // Not an edge case — the outcome BR-SAL-016 explicitly predicts. Drafts do not reserve, so
        // both documents pass the add-line ceiling against the same remainder and the second is
        // refused at commit. It must be refused as an **over-return**, which is what actually
        // happened and what the screen has a message for; reporting it as an unreadable trace would
        // misdiagnose "someone returned it first" as "the ledger is broken".
        var seed = await SoldAsync(cartons: 1m, boxes: 10m);

        var first = await NewReturnAsync(seed.InvoiceId);
        var second = await NewReturnAsync(seed.InvoiceId);

        // Both additions are accepted: neither draft holds anything back from the other.
        (await AddReturnLineAsync(first, seed.SaleLineId, 6m)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await AddReturnLineAsync(second, seed.SaleLineId, 6m)).StatusCode.ShouldBe(HttpStatusCode.Created);

        (await CommitAsync(first)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var rejected = await CommitAsync(second);
        rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(rejected)).ShouldBe(SalesErrorCodes.ReturnQuantityExceedsReturnable);

        // The first return's 6 boxes are the whole effect; the second changed nothing and stays a draft.
        (await ReturnAsync(second)).Status.ShouldBe(SalesReturnStatus.Draft);
        (await MovementsAsync(seed.BatchId, InventoryMovementType.SalesReturn)).Count.ShouldBe(1);
        (await RemainingAsync(seed.BatchId))
            .ShouldBe(StripsPerCarton - (10m * StripsPerBox) + (6m * StripsPerBox));
    }

    [Fact]
    public async Task An_empty_return_cannot_be_committed_BR_SAL_018()
    {
        var seed = await SoldAsync(cartons: 1m, boxes: 3m);
        var returnId = await NewReturnAsync(seed.InvoiceId);

        var rejected = await CommitAsync(returnId);

        rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(rejected)).ShouldBe(SalesErrorCodes.ReturnHasNoLines);
    }

    [Fact]
    public async Task A_missing_original_invoice_is_a_per_field_rejection_AC_SAL_014()
    {
        // The validator only exists if it is registered: resolved with GetServices, an unregistered
        // one means no validation at all, and a missing id would reach the handler's `!` and surface
        // as a 500. This pins the documented per-field 400 instead.
        var response = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/sales-returns", UriKind.Relative),
            new { returnDate = fixture.ClinicToday });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("salesInvoiceId", Case.Insensitive);
    }

    [Fact]
    public async Task A_return_without_a_consumption_trace_fails_loudly_BR_SAL_017()
    {
        // A sale committed before the movement ledger existed has no Consume rows — the C1 migration
        // replaced the Sprint 7 consumption table without backfilling it. There is then no way to
        // know which batch the goods left, and BR-SAL-017 forbids inventing one, so the commit is
        // rejected with nothing saved rather than guessing a destination.
        var seed = await SoldAsync(cartons: 1m, boxes: 4m);
        await EraseConsumptionTraceAsync(seed.SaleLineId);

        var returnId = await NewReturnAsync(seed.InvoiceId);
        await AddReturnLineAsync(returnId, seed.SaleLineId, 2m);

        var rejected = await CommitAsync(returnId);

        rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(rejected)).ShouldBe(SalesErrorCodes.ReturnConsumptionTraceUnusable);

        // Nothing was applied and the document stayed a draft: the transition and the stock effect
        // share one unit of work (BR-INV-062).
        (await ReturnAsync(returnId)).Status.ShouldBe(SalesReturnStatus.Draft);
        (await MovementsAsync(seed.BatchId, InventoryMovementType.SalesReturn)).ShouldBeEmpty();
        (await RemainingAsync(seed.BatchId)).ShouldBe(StripsPerCarton - (4m * StripsPerBox));
    }

    // ---- seeding and helpers ----------------------------------------------------------------------

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>Seed a product and receive <paramref name="cartons"/> of it through the real receiving path.</summary>
    private async Task<(Guid ProductId, Guid BatchId)> ReceivedAsync(decimal cartons, DateOnly? expiry = null)
    {
        var marker = Marker();
        Guid productId = Guid.Empty;
        Guid invoiceId = Guid.Empty;

        await fixture.SeedAsync(async dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            var product = CatalogSeeder.NewProduct(
                dbContext, $"منتج {marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature,
                hasExpiration: true, boxPrice: 50m, stripPrice: 6m);
            var invoice = await PurchasingSeeder.NewInvoiceAsync(
                dbContext, $"مورد {marker}", new DateOnly(2026, 5, 1), 0m);
            productId = product.Id;
            invoiceId = invoice.Id;
        });

        var batchId = await ReceiveIntoAsync(invoiceId, productId, cartons, expiry);
        return (productId, batchId);
    }

    private async Task<Guid> ReceiveIntoAsync(Guid invoiceId, Guid productId, decimal cartons, DateOnly? expiry)
    {
        var lineResponse = await fixture.Client.PostAsJsonAsync(
            new Uri($"/api/v1/purchase-invoices/{invoiceId}/lines", UriKind.Relative),
            new { productId, purchaseUnitId = SeededCatalogIds.CartonUnit, quantity = cartons, unitPrice = 400m });
        lineResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var lineBody = JsonDocument.Parse(await lineResponse.Content.ReadAsStringAsync());
        var lineId = lineBody.RootElement.GetProperty("lineId").GetGuid();

        var receive = await fixture.Client.PostAsJsonAsync(
            new Uri($"/api/v1/purchase-invoices/{invoiceId}/receive", UriKind.Relative),
            new { lines = new[] { new { lineId, expiryDate = expiry ?? fixture.ClinicToday.AddDays(180) } } });
        receive.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        return await fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.PurchaseLineId == lineId).Select(batch => batch.Id).SingleAsync());
    }

    /// <summary>Receive stock, then sell <paramref name="boxes"/> of it through the real commit path.</summary>
    private async Task<(Guid ProductId, Guid BatchId, Guid InvoiceId, Guid SaleLineId, string CustomerName)>
        SoldAsync(decimal cartons, decimal boxes, DateOnly? expiry = null)
    {
        var stock = await ReceivedAsync(cartons, expiry);
        var customerName = $"عميل {Marker()}";

        var invoiceId = await CreateInvoiceAsync(customerName);
        var saleLineId = await AddSaleLineAsync(invoiceId, stock.ProductId, boxes);
        (await CommitSaleAsync(invoiceId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        return (stock.ProductId, stock.BatchId, invoiceId, saleLineId, customerName);
    }

    /// <summary>
    /// Two batches of one product, the first expiring sooner, and a single sale line big enough that
    /// FEFO must split across both — the situation BR-SAL-017 exists for.
    /// </summary>
    private async Task<(Guid ProductId, Guid BatchA, Guid BatchB, Guid InvoiceId, Guid SaleLineId)>
        SoldAcrossTwoBatchesAsync()
    {
        var first = await ReceivedAsync(cartons: 1m, expiry: fixture.ClinicToday.AddDays(30));

        Guid secondInvoiceId = Guid.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var invoice = await PurchasingSeeder.NewInvoiceAsync(
                dbContext, $"مورد {Marker()}", new DateOnly(2026, 5, 2), 0m);
            secondInvoiceId = invoice.Id;
        });
        var batchB = await ReceiveIntoAsync(
            secondInvoiceId, first.ProductId, cartons: 1m, expiry: fixture.ClinicToday.AddDays(90));

        // 15 boxes = 150 strips: all 120 of batch A (nearer expiry) and 30 of batch B.
        var invoiceId = await CreateInvoiceAsync($"عميل {Marker()}");
        var saleLineId = await AddSaleLineAsync(invoiceId, first.ProductId, 15m);
        (await CommitSaleAsync(invoiceId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await RemainingAsync(first.BatchId)).ShouldBe(0m);
        (await RemainingAsync(batchB)).ShouldBe(90m);

        return (first.ProductId, first.BatchId, batchB, invoiceId, saleLineId);
    }

    private async Task<Guid> CreateInvoiceAsync(string? customerName = null)
    {
        var response = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/sales-invoices", UriKind.Relative),
            new { saleDate = fixture.ClinicToday, customerName });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<Guid> AddSaleLineAsync(Guid invoiceId, Guid productId, decimal boxes)
    {
        var response = await fixture.Client.PostAsJsonAsync(
            new Uri($"/api/v1/sales-invoices/{invoiceId}/lines", UriKind.Relative),
            new { productId, saleUnitId = SeededCatalogIds.BoxUnit, quantity = boxes });
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("lineId").GetGuid();
    }

    private Task<HttpResponseMessage> CommitSaleAsync(Guid invoiceId) =>
        fixture.Client.PostAsync(
            new Uri($"/api/v1/sales-invoices/{invoiceId}/commit", UriKind.Relative), content: null);

    private Task<HttpResponseMessage> CreateReturnAsync(Guid invoiceId) =>
        fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/sales-returns", UriKind.Relative),
            new { salesInvoiceId = invoiceId, returnDate = fixture.ClinicToday, notes = (string?)null });

    private async Task<Guid> NewReturnAsync(Guid invoiceId)
    {
        var response = await CreateReturnAsync(invoiceId);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> AddReturnLineAsync(Guid returnId, Guid salesLineItemId, decimal quantity) =>
        fixture.Client.PostAsJsonAsync(
            new Uri($"/api/v1/sales-returns/{returnId}/lines", UriKind.Relative),
            new { salesLineItemId, quantity });

    private async Task<Guid> AddReturnLineIdAsync(Guid returnId, Guid salesLineItemId, decimal quantity)
    {
        var response = await AddReturnLineAsync(returnId, salesLineItemId, quantity);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> CommitAsync(Guid returnId) =>
        fixture.Client.PostAsync(
            new Uri($"/api/v1/sales-returns/{returnId}/commit", UriKind.Relative), content: null);

    private async Task<JsonElement[]> ReturnableAsync(Guid invoiceId)
    {
        var response = await fixture.Client.GetAsync(ReturnableUri(invoiceId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. body.RootElement.EnumerateArray().Select(element => element.Clone())];
    }

    private static Uri ReturnableUri(Guid invoiceId) =>
        new($"/api/v1/sales-invoices/{invoiceId}/returnable-lines", UriKind.Relative);

    private async Task<decimal> InvoiceTotalAsync(Guid invoiceId)
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/sales-invoices/{invoiceId}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("total").GetProperty("amount").GetDecimal();
    }

    private Task<SalesReturn> ReturnAsync(Guid returnId) =>
        fixture.QueryDbAsync(dbContext => dbContext.SalesReturns
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

    private Task ExpireBatchAsync(Guid batchId) =>
        fixture.SeedAsync(dbContext => dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE inventory_batches SET expiry_date = CURRENT_DATE - 1 WHERE id = {0}", batchId));

    private Task EraseConsumptionTraceAsync(Guid saleLineId) =>
        fixture.SeedAsync(dbContext => dbContext.InventoryMovements
            .Where(movement => movement.ReferenceId == saleLineId
                && movement.Type == InventoryMovementType.Consume)
            .ExecuteDeleteAsync());

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("errorCode").GetString();
    }
}
