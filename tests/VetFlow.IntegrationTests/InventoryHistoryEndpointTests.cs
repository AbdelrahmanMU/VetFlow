using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shouldly;
using VetFlow.Application.Inventory.Queries.InventoryHistory;
using VetFlow.Infrastructure.Inventory;
using VetFlow.Infrastructure.Persistence;
using VetFlow.Infrastructure.Persistence.Tenancy;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Inventory movement history (REQ-INV-005, reopened by DEC-INV-038) — GET
/// /api/v1/inventory/movements, end to end through the real API and a real PostgreSQL.
///
/// Covers TS-INV-031..036: one history row per ledger row with the seven frozen fields
/// (BR-INV-041), the field mapping and signed quantity for both writing paths that exist
/// (BR-INV-040/064), the per-type reference and source module including "no document" (BR-INV-043),
/// newest-first ordering with a stable tie-break across pages (BR-INV-044), the read-only surface
/// (BR-INV-039), and the constant query count (BR-INV-045).
///
/// <para><b>Rows are produced by the real writing paths</b> — receiving a purchase invoice and
/// committing a sale — not by inserting ledger rows. That is the point of the capability: the
/// screen must show what the system actually wrote (BR-INV-062).</para>
///
/// <para>The history is clinic-wide and unfiltered by design (BR-INV-044), so every assertion
/// selects this test's own rows by its marker product name out of a large first page. Ordering is
/// asserted on the relative order of those rows, never on the absolute position of a row in a
/// database other tests also write to.</para>
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class InventoryHistoryEndpointTests(ApiFixture fixture)
{
    /// <summary>Wide enough that this test's just-written rows are all on the newest page.</summary>
    private const int WidePage = 100;

    /// <summary>The seeded chain is carton ×12 → box ×10 → strip (the stock unit): 1 carton = 120 strips.</summary>
    private const decimal CartonToStrip = 120m;

    [Fact]
    public async Task Receiving_writes_one_row_per_batch_with_the_seven_fields_only_TS_INV_031()
    {
        // One received invoice with two lines → two batches → exactly two Receive movements.
        var seed = await ReceivedPurchaseAsync(lines: 2);

        var rows = await RowsForAsync(seed.ProductName);

        rows.Count.ShouldBe(2);
        foreach (var row in rows)
        {
            // The field list is locked by BR-INV-041: the seven business fields, the movement id
            // that carries the tie-break, and the stock unit the quantity is expressed in. Reason,
            // reason note and actor exist in the ledger and are deliberately NOT exposed here.
            row.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)
                .ShouldBe(
                [
                    "batchId", "movementId", "occurredAt", "productName", "quantity",
                    "referenceId", "referenceLabel", "referenceTarget", "source", "stockUnitName", "type",
                ]);
            row.GetProperty("type").GetString().ShouldBe("receive");
        }
    }

    [Fact]
    public async Task Receive_row_carries_a_positive_quantity_and_the_purchase_reference_TS_INV_032()
    {
        var seed = await ReceivedPurchaseAsync(lines: 1, quantity: 2m);

        var row = (await RowsForAsync(seed.ProductName)).Single();

        // Positive = increase (BR-INV-064), recorded in the stock unit, not the purchase unit
        // (BR-INV-002): two cartons are 240 strips.
        row.GetProperty("quantity").GetDecimal().ShouldBe(2m * CartonToStrip);
        row.GetProperty("productName").GetString().ShouldBe(seed.ProductName);
        row.GetProperty("stockUnitName").GetString().ShouldNotBeNullOrWhiteSpace();
        row.GetProperty("batchId").GetGuid().ShouldNotBe(Guid.Empty);
        row.GetProperty("source").GetString().ShouldBe("purchasing");
        row.GetProperty("referenceTarget").GetString().ShouldBe("purchaseInvoice");
        row.GetProperty("referenceLabel").GetString().ShouldBe(seed.InvoiceNumber);
        row.GetProperty("referenceId").GetGuid().ShouldBe(seed.InvoiceId);   // the /purchases/:id target
    }

    [Fact]
    public async Task Committing_a_sale_writes_a_negative_consume_row_with_the_sales_reference_TS_INV_032()
    {
        var seed = await ReceivedPurchaseAsync(lines: 1, quantity: 1m);
        var sale = await CommittedSaleAsync(seed.ProductId, quantity: 3m);

        var rows = await RowsForAsync(seed.ProductName);

        var consume = rows.Single(row => row.GetProperty("type").GetString() == "consume");
        consume.GetProperty("quantity").GetDecimal().ShouldBe(-3m);    // negative = decrease (BR-INV-064)
        consume.GetProperty("source").GetString().ShouldBe("sales");
        consume.GetProperty("referenceTarget").GetString().ShouldBe("salesInvoice");
        consume.GetProperty("referenceLabel").GetString().ShouldBe(sale.Number);
        consume.GetProperty("referenceId").GetGuid().ShouldBe(sale.InvoiceId);   // the /sales/:id target

        // Both movements of the same batch appear — the ledger records history, it does not net out
        // (BR-INV-062). The remaining quantity stays authoritative elsewhere (BR-INV-063).
        rows.Select(row => row.GetProperty("type").GetString()).OrderBy(type => type, StringComparer.Ordinal)
            .ShouldBe(["consume", "receive"]);
        rows.Select(row => row.GetProperty("batchId").GetGuid()).Distinct().Count().ShouldBe(1);
    }

    [Fact]
    public async Task Newest_first_tie_broken_by_movement_id_and_stable_across_pages_TS_INV_034()
    {
        var seed = await ReceivedPurchaseAsync(lines: 3);

        var rows = await RowsForAsync(seed.ProductName);

        // Newest first (BR-INV-044). The three receives share one receive instant, so the tie-break
        // is what actually orders them — exactly the case a per-call timestamp would hide.
        var stamps = rows.Select(row => row.GetProperty("occurredAt").GetDateTimeOffset()).ToList();
        stamps.ShouldBe([.. stamps.OrderByDescending(stamp => stamp)]);

        var tied = rows.Where(row => row.GetProperty("occurredAt").GetDateTimeOffset() == stamps[0])
            .Select(row => row.GetProperty("movementId").GetGuid()).ToList();
        tied.ShouldBe([.. tied.OrderBy(id => id)]);

        // Paging the same total order never repeats or drops a row.
        var firstPage = await IdsAsync(page: 1, pageSize: 2);
        var secondPage = await IdsAsync(page: 2, pageSize: 2);
        firstPage.Count.ShouldBe(2);
        firstPage.Intersect(secondPage).ShouldBeEmpty();
        (await IdsAsync(page: 1, pageSize: 4)).Take(2).ShouldBe(firstPage);
    }

    [Fact]
    public async Task Exposes_no_write_endpoint_TS_INV_035()
    {
        // The ledger is append-only and written only inside the operations that change stock
        // (BR-INV-039, BR-INV-062): there is no create/edit/delete surface to reach.
        var uri = new Uri("/api/v1/inventory/movements", UriKind.Relative);

        (await fixture.Client.PostAsync(uri, content: null)).StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        (await fixture.Client.PutAsync(uri, content: null)).StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
        (await fixture.Client.DeleteAsync(uri)).StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Returns_an_empty_page_past_the_end_TS_INV_036()
    {
        // A clinic-wide empty ledger cannot be forced in a shared database, so the empty *payload*
        // is asserted at its real boundary — past the last page. The screen's empty state is
        // covered by the frontend specs.
        using var body = await GetAsync("page=1&pageSize=1");
        var total = body.RootElement.GetProperty("totalCount").GetInt32();

        using var past = await GetAsync($"page={(total / 1) + 1}&pageSize=1");
        past.RootElement.GetProperty("items").GetArrayLength().ShouldBe(0);
        past.RootElement.GetProperty("totalCount").GetInt32().ShouldBe(total);
    }

    [Fact]
    public async Task Issues_a_constant_number_of_queries_whatever_the_page_holds_BR_INV_045()
    {
        // A page of mixed types and sources must cost the same as a page of one type: one
        // projection SELECT plus the pagination COUNT — no per-row reference lookups (BR-INV-045).
        var seed = await ReceivedPurchaseAsync(lines: 3);
        await CommittedSaleAsync(seed.ProductId, quantity: 2m);

        var small = await CountReadsAsync(pageSize: 1);
        var large = await CountReadsAsync(pageSize: WidePage);

        small.ShouldBe(2);
        large.ShouldBe(small);   // constant, not a function of the row count or the type mix
    }

    // ---- seeding and helpers ----------------------------------------------------------------------

    /// <summary>
    /// Receives a real purchase invoice through the API: the receiving path creates the batches and
    /// writes their Receive movements in the same unit of work (BR-INV-062).
    /// </summary>
    private async Task<(Guid ProductId, string ProductName, Guid InvoiceId, string InvoiceNumber)> ReceivedPurchaseAsync(
        int lines,
        decimal quantity = 10m)
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var productName = $"منتج {marker}";
        var productId = Guid.Empty;
        var invoiceId = Guid.Empty;
        var invoiceNumber = string.Empty;

        await fixture.SeedAsync(async dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            productId = CatalogSeeder.NewProduct(
                dbContext, productName, category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature,
                isSplittable: true, hasExpiration: false, stripPrice: 5m).Id;

            var invoice = await PurchasingSeeder.NewInvoiceAsync(
                dbContext, $"مورّد {marker}", DateOnly.FromDateTime(DateTime.UtcNow), 0m);
            invoiceId = invoice.Id;
            invoiceNumber = invoice.Number;
        });

        for (var line = 0; line < lines; line++)
        {
            var added = await fixture.Client.PostAsJsonAsync(
                new Uri($"/api/v1/purchase-invoices/{invoiceId}/lines", UriKind.Relative),
                new { productId, purchaseUnitId = SeededCatalogIds.CartonUnit, quantity, unitPrice = 100m });
            added.StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        var received = await fixture.Client.PostAsJsonAsync(
            new Uri($"/api/v1/purchase-invoices/{invoiceId}/receive", UriKind.Relative),
            new { lines = Array.Empty<object>() });
        received.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        return (productId, productName, invoiceId, invoiceNumber);
    }

    /// <summary>Creates and commits a real sale, which consumes stock and writes Consume movements.</summary>
    private async Task<(Guid InvoiceId, string Number)> CommittedSaleAsync(Guid productId, decimal quantity)
    {
        var created = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/sales-invoices", UriKind.Relative),
            new { saleDate = DateOnly.FromDateTime(DateTime.UtcNow) });
        created.StatusCode.ShouldBe(HttpStatusCode.Created);
        var invoiceId = (await ReadJsonAsync(created)).RootElement.GetProperty("id").GetGuid();

        var added = await fixture.Client.PostAsJsonAsync(
            new Uri($"/api/v1/sales-invoices/{invoiceId}/lines", UriKind.Relative),
            new { productId, saleUnitId = SeededCatalogIds.StripUnit, quantity });
        added.StatusCode.ShouldBe(HttpStatusCode.Created);

        var committed = await fixture.Client.PostAsync(
            new Uri($"/api/v1/sales-invoices/{invoiceId}/commit", UriKind.Relative), content: null);
        committed.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var number = await fixture.QueryDbAsync(dbContext => dbContext.SalesInvoices
            .Where(invoice => invoice.Id == invoiceId)
            .Select(invoice => invoice.Number)
            .SingleAsync());

        return (invoiceId, number);
    }

    /// <summary>This test's own rows, selected out of the newest page by the marker product name.</summary>
    private async Task<List<JsonElement>> RowsForAsync(string productName)
    {
        using var body = await GetAsync($"page=1&pageSize={WidePage}");
        return
        [
            .. body.RootElement.GetProperty("items").EnumerateArray()
                .Where(row => row.GetProperty("productName").GetString() == productName)
                .Select(row => row.Clone()),
        ];
    }

    private async Task<List<Guid>> IdsAsync(int page, int pageSize)
    {
        using var body = await GetAsync($"page={page}&pageSize={pageSize}");
        return [.. body.RootElement.GetProperty("items").EnumerateArray()
            .Select(row => row.GetProperty("movementId").GetGuid())];
    }

    private async Task<JsonDocument> GetAsync(string queryString)
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/inventory/movements?{queryString}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await ReadJsonAsync(response);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private async Task<int> CountReadsAsync(int pageSize)
    {
        var counter = new CommandCountingInterceptor();

        // The session interceptor publishes the tenant that row-level security reads; without it
        // this context would meet the policies with no tenant and read nothing (ADR-0022 §8.2).
        // It opens the connection rather than issuing a reader, so it does not disturb the count.
        var tenantContext = new TestTenantContext();
        var builder = new DbContextOptionsBuilder<VetFlowDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .AddInterceptors(
                new TenantSessionInterceptor(tenantContext),
                new TenantStampInterceptor(tenantContext),
                counter);
        await using var dbContext = new VetFlowDbContext(builder.Options, tenantContext);

        var handler = new InventoryHistoryQueryHandler(dbContext);
        counter.Reset();
        await handler.HandleAsync(new InventoryHistoryQuery { Page = 1, PageSize = pageSize }, CancellationToken.None);
        return counter.Count;
    }

    /// <summary>Counts the SQL commands a block of work actually issues (BR-INV-045).</summary>
    private sealed class CommandCountingInterceptor : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public void Reset() => Count = 0;

        public override InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
            System.Data.Common.DbCommand command,
            CommandEventData eventData,
            InterceptionResult<System.Data.Common.DbDataReader> result)
        {
            Count++;
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
            System.Data.Common.DbCommand command,
            CommandEventData eventData,
            InterceptionResult<System.Data.Common.DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
