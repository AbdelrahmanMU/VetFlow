using System.Globalization;
using System.Net;
using System.Text.Json;
using Shouldly;
using VetFlow.Domain.Purchasing;
using VetFlow.Domain.Sales;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The operational dashboard read path (REQ-DSH-010) — GET /api/v1/dashboard. One request
/// carries every section, each section carries its own outcome (DEC-DSH-002), and every
/// number is produced by the module that owns its rule (BR-DSH-001, DEC-DSH-001).
/// <para>
/// <b>Counts here are clinic-wide, and this database is shared across the whole test
/// collection.</b> So nothing below asserts an absolute total. Behaviour is asserted as a
/// <i>delta</i> around seeding, and correctness is asserted as <i>parity</i> with the screen
/// each tile links to — which is AC-DSH-020 / AC-INV-069 stated exactly as the criterion
/// words it, and is a stronger check than any fixed number would be.
/// </para>
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class DashboardEndpointTests(ApiFixture fixture)
{
    // ---------------------------------------------------------------- shape and access

    [Fact]
    public async Task Returns_every_section_in_one_request_REQ_DSH_010()
    {
        var dashboard = await GetDashboardAsync();

        var sections = dashboard.GetProperty("sections");
        foreach (var key in AllSectionKeys)
        {
            // A section is never omitted: an absent key and a zero would be
            // indistinguishable to the client (BR-DSH-014).
            sections.TryGetProperty(key, out var section).ShouldBeTrue($"section '{key}' is missing");
            section.GetProperty("status").GetString().ShouldBe("ok");
        }

        // The server states the date it used, so the browser never derives one — device time
        // is a prohibited source (BR-DSH-003, clinic-date.md).
        dashboard.GetProperty("clinicDate").GetString()
            .ShouldBe(fixture.ClinicToday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Carries_no_business_action_and_no_write_surface_BR_DSH_020()
    {
        var response = await fixture.Client.PostAsync(
            new Uri("/api/v1/dashboard", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task Low_stock_is_absent_entirely_DEC_DSH_005()
    {
        var dashboard = await GetDashboardAsync();

        // Blocked, not rejected: BR-INV-012 is deferred and DEC-INV-004 forbids inventing a
        // general threshold. The guard is that NO low-stock notion exists on this payload —
        // not even a zero, which is how a placeholder would look.
        var payload = dashboard.GetRawText();
        payload.ShouldNotContain("lowStock", Case.Insensitive);
        payload.ShouldNotContain("reorder", Case.Insensitive);
    }

    // ---------------------------------------------------------------- expiry counts

    [Fact]
    public async Task Counts_expired_and_expiring_soon_batches_AC_INV_066()
    {
        var before = await GetDashboardAsync();

        var name = $"منتج-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            var productId = NewProduct(dbContext, name);
            // Two expired, one expiring inside the 30-day horizon, one far outside it.
            await InventorySeeder.AddBatchWithProvenanceAsync(
                dbContext, productId, name, 5m, Today().AddDays(-3));
            await InventorySeeder.AddBatchWithProvenanceAsync(
                dbContext, productId, name, 5m, Today().AddDays(-1));
            await InventorySeeder.AddBatchWithProvenanceAsync(
                dbContext, productId, name, 5m, Today().AddDays(10));
            await InventorySeeder.AddBatchWithProvenanceAsync(
                dbContext, productId, name, 5m, Today().AddDays(90));
            InventorySeeder.SetOnHand(dbContext, productId, 20m);
        });

        var after = await GetDashboardAsync();

        Delta(before, after, "expiredBatches").ShouldBe(2);
        Delta(before, after, "expiringSoonBatches").ShouldBe(1);
    }

    [Fact]
    public async Task A_batch_expiring_today_is_expiring_soon_and_not_expired_AC_INV_066()
    {
        var before = await GetDashboardAsync();

        var name = $"منتج-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            var productId = NewProduct(dbContext, name);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 5m, Today());
            InventorySeeder.SetOnHand(dbContext, productId, 5m);
        });

        var after = await GetDashboardAsync();

        // ExpiryDate is the LAST saleable day (BR-INV-059) — a batch dated today is still
        // sellable, so counting it as expired would withdraw stock a day early.
        Delta(before, after, "expiredBatches").ShouldBe(0);
        Delta(before, after, "expiringSoonBatches").ShouldBe(1);
    }

    [Fact]
    public async Task Batches_without_expiry_and_depleted_batches_are_counted_in_neither_AC_INV_066()
    {
        var before = await GetDashboardAsync();

        var name = $"منتج-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            var productId = NewProduct(dbContext, name);

            // No expiry at all — nothing to monitor (BR-INV-033).
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 5m);

            // Depleted and long expired — nothing left to lose, so out of scope.
            var (depleted, _) = await InventorySeeder.AddBatchWithProvenanceAsync(
                dbContext, productId, name, 5m, Today().AddDays(-30));
            InventorySeeder.MarkDepleted(dbContext, depleted);

            InventorySeeder.SetOnHand(dbContext, productId, 5m);
        });

        var after = await GetDashboardAsync();

        Delta(before, after, "expiredBatches").ShouldBe(0);
        Delta(before, after, "expiringSoonBatches").ShouldBe(0);
    }

    // ---------------------------------------------------------------- out of stock

    [Fact]
    public async Task Counts_products_at_zero_and_ignores_products_never_received_AC_INV_067()
    {
        var before = await GetDashboardAsync();

        await fixture.SeedAsync(async dbContext =>
        {
            var depletedName = $"منتج-{Marker()}";
            var depletedId = NewProduct(dbContext, depletedName);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, depletedId, depletedName, 4m);
            InventorySeeder.SetOnHand(dbContext, depletedId, 0m);

            var stockedName = $"منتج-{Marker()}";
            var stockedId = NewProduct(dbContext, stockedName);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, stockedId, stockedName, 7m);
            InventorySeeder.SetOnHand(dbContext, stockedId, 7m);

            // Never received: no ProductOnHand row at all. It must not be counted — and it is
            // absent from the destination screen for the same reason (BR-INV-007,
            // DEC-INV-003). This is why the tile says «نفد مخزونها», not «لا يمكن بيعها».
            NewProduct(dbContext, $"منتج-{Marker()}");
        });

        var after = await GetDashboardAsync();

        Delta(before, after, "outOfStockProducts").ShouldBe(1);
    }

    // ---------------------------------------------------------------- drafts

    [Fact]
    public async Task Counts_draft_purchase_invoices_only_AC_PUR_026()
    {
        var before = await GetDashboardAsync();

        await fixture.SeedAsync(async dbContext =>
        {
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد-{Marker()}", Today(), 100m);
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد-{Marker()}", Today(), 100m);

            var received = await PurchasingSeeder.NewInvoiceAsync(
                dbContext, $"مورد-{Marker()}", Today(), 100m);
            PurchasingSeeder.SetStatus(dbContext, received, PurchaseInvoiceStatus.Received);

            var cancelled = await PurchasingSeeder.NewInvoiceAsync(
                dbContext, $"مورد-{Marker()}", Today(), 100m);
            PurchasingSeeder.SetStatus(dbContext, cancelled, PurchaseInvoiceStatus.Cancelled);
        });

        var after = await GetDashboardAsync();

        Delta(before, after, "draftPurchases").ShouldBe(2);
    }

    [Fact]
    public async Task Counts_draft_sales_invoices_only_AC_SAL_023()
    {
        var before = await GetDashboardAsync();

        await fixture.SeedAsync(async dbContext =>
        {
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل-{Marker()}", Today(), 50m);

            var committed = await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل-{Marker()}", Today(), 50m);
            SalesSeeder.SetStatus(dbContext, committed, SalesInvoiceStatus.Committed);
        });

        var after = await GetDashboardAsync();

        Delta(before, after, "draftSales").ShouldBe(1);
    }

    // ---------------------------------------------------------------- today's sales

    [Fact]
    public async Task Todays_sales_counts_committed_invoices_dated_today_and_sums_their_totals_AC_SAL_023()
    {
        var before = await GetDashboardAsync();

        await fixture.SeedAsync(async dbContext =>
        {
            var first = await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل-{Marker()}", Today(), 120.50m);
            SalesSeeder.SetStatus(dbContext, first, SalesInvoiceStatus.Committed);

            var second = await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل-{Marker()}", Today(), 79.50m);
            SalesSeeder.SetStatus(dbContext, second, SalesInvoiceStatus.Committed);
        });

        var after = await GetDashboardAsync();

        TodaySales(after).Count.ShouldBe(TodaySales(before).Count + 2);

        // Each total was rounded by Sales at commit time (DEC-SAL-004); the dashboard only
        // adds them, and rounds nothing of its own.
        (TodaySales(after).Total - TodaySales(before).Total).ShouldBe(200.00m);
    }

    [Fact]
    public async Task A_draft_dated_today_is_excluded_from_todays_sales_AC_SAL_023()
    {
        var before = await GetDashboardAsync();

        await fixture.SeedAsync(async dbContext =>
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل-{Marker()}", Today(), 500m));

        var after = await GetDashboardAsync();

        // A draft consumed no stock and recorded no revenue (BR-SAL-009..012); it is counted
        // in its own tile instead.
        TodaySales(after).Count.ShouldBe(TodaySales(before).Count);
        TodaySales(after).Total.ShouldBe(TodaySales(before).Total);
        Delta(before, after, "draftSales").ShouldBe(1);
    }

    [Fact]
    public async Task An_invoice_back_dated_to_yesterday_is_excluded_even_if_committed_today_AC_SAL_025()
    {
        var before = await GetDashboardAsync();

        var customer = $"عميل-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            var invoice = await SalesSeeder.NewInvoiceAsync(
                dbContext, customer, Today().AddDays(-1), 999m);
            SalesSeeder.SetStatus(dbContext, invoice, SalesInvoiceStatus.Committed);
        });

        var after = await GetDashboardAsync();

        // The reference is the header sale date, not the commit time — the field BR-SAL-019
        // filters by. Taking commit time instead would count it here while the screen this
        // tile links to would not show it.
        TodaySales(after).Count.ShouldBe(TodaySales(before).Count);

        var listed = await ListTotalCountAsync(
            $"/api/v1/sales-invoices?dateFrom={Iso(Today())}&dateTo={Iso(Today())}"
            + $"&search={Uri.EscapeDataString(customer)}");
        listed.ShouldBe(0);
    }

    // ---------------------------------------------------------------- recent movements

    [Fact]
    public async Task Recent_movements_are_the_latest_five_in_ledger_order_AC_INV_068()
    {
        var movements = (await GetDashboardAsync())
            .GetProperty("sections").GetProperty("recentMovements");

        movements.GetProperty("status").GetString().ShouldBe("ok");

        var items = movements.GetProperty("items").EnumerateArray().ToList();
        items.Count.ShouldBeLessThanOrEqualTo(5);

        // Newest first (BR-INV-044) — the same order the history screen uses, so "the latest
        // five" means the same thing in both places.
        var timestamps = items.Select(item => item.GetProperty("occurredAt").GetDateTimeOffset()).ToList();
        timestamps.ShouldBe(timestamps.OrderByDescending(timestamp => timestamp).ToList());

        // Four fields (BR-DSH-010) — deliberately narrower than the history screen's seven
        // (BR-INV-041), because the dashboard hands off to that screen rather than copying it.
        foreach (var item in items)
        {
            item.GetProperty("type").GetString().ShouldNotBeNullOrWhiteSpace();
            item.GetProperty("productName").GetString().ShouldNotBeNullOrWhiteSpace();
            item.GetProperty("stockUnitName").GetString().ShouldNotBeNullOrWhiteSpace();
            item.TryGetProperty("batchId", out _).ShouldBeFalse();
            item.TryGetProperty("referenceLabel", out _).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task Recent_movements_agree_with_the_history_screen_AC_INV_068()
    {
        var dashboardTypes = (await GetDashboardAsync())
            .GetProperty("sections").GetProperty("recentMovements")
            .GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("movementId").GetGuid())
            .ToList();

        var response = await fixture.Client.GetAsync(
            new Uri("/api/v1/inventory/movements?pageSize=5", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var historyIds = body.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("movementId").GetGuid())
            .ToList();

        dashboardTypes.ShouldBe(historyIds);
    }

    // ---------------------------------------------------------------- population parity

    /// <summary>
    /// AC-DSH-020 / AC-INV-069 / AC-PUR-027 / AC-SAL-025, stated as the criteria word it:
    /// <b>each dashboard count equals the row count of the screen its tile links to, after the
    /// same filter, on the same data.</b>
    /// <para>
    /// This is the AC-INV-065 pattern and it exists for the same reason: EF cannot share a
    /// predicate inside an expression tree, so the counting condition necessarily exists in
    /// two places. A comment cannot stop those two from drifting — <b>this test can</b>.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Every_count_equals_its_destination_screens_row_count_AC_DSH_020()
    {
        // Seed a little of everything first, so the parity check runs against a non-trivial
        // population rather than passing vacuously on zeros.
        var name = $"منتج-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            var productId = NewProduct(dbContext, name);
            await InventorySeeder.AddBatchWithProvenanceAsync(
                dbContext, productId, name, 5m, Today().AddDays(-2));
            await InventorySeeder.AddBatchWithProvenanceAsync(
                dbContext, productId, name, 5m, Today().AddDays(7));
            InventorySeeder.SetOnHand(dbContext, productId, 10m);

            var emptyName = $"منتج-{Marker()}";
            var emptyId = NewProduct(dbContext, emptyName);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, emptyId, emptyName, 1m);
            InventorySeeder.SetOnHand(dbContext, emptyId, 0m);

            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد-{Marker()}", Today(), 10m);
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل-{Marker()}", Today(), 10m);
        });

        var sections = (await GetDashboardAsync()).GetProperty("sections");

        (int Count, string Url)[] parities =
        [
            (Count(sections, "expiredBatches"), "/api/v1/inventory/expiry?expired=true"),
            (Count(sections, "expiringSoonBatches"), "/api/v1/inventory/expiry?expiringSoon=true"),
            (Count(sections, "outOfStockProducts"), "/api/v1/inventory?outOfStock=true"),
            (Count(sections, "draftPurchases"), "/api/v1/purchase-invoices?status=draft"),
            (Count(sections, "draftSales"), "/api/v1/sales-invoices?status=draft"),
            (sections.GetProperty("todaySales").GetProperty("count").GetInt32(),
                $"/api/v1/sales-invoices?status=committed&dateFrom={Iso(Today())}&dateTo={Iso(Today())}"),
        ];

        foreach (var (count, url) in parities)
        {
            var listed = await ListTotalCountAsync(url);
            count.ShouldBe(listed, $"dashboard count disagrees with {url}");
        }
    }

    // ---------------------------------------------------------------- helpers

    private static readonly string[] AllSectionKeys =
    [
        "expiredBatches", "outOfStockProducts", "expiringSoonBatches",
        "draftPurchases", "draftSales", "todaySales", "recentMovements",
    ];

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];

    private DateOnly Today() => fixture.ClinicToday;

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static int Count(JsonElement sections, string key) =>
        sections.GetProperty(key).GetProperty("count").GetInt32();

    private static int Delta(JsonElement before, JsonElement after, string key) =>
        Count(after.GetProperty("sections"), key) - Count(before.GetProperty("sections"), key);

    private static (int Count, decimal Total) TodaySales(JsonElement dashboard)
    {
        var section = dashboard.GetProperty("sections").GetProperty("todaySales");
        return (
            section.GetProperty("count").GetInt32(),
            section.GetProperty("total").GetProperty("amount").GetDecimal());
    }

    private static Guid NewProduct(Infrastructure.Persistence.VetFlowDbContext dbContext, string name)
    {
        var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف-{Guid.NewGuid():N}");
        var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع-{Guid.NewGuid():N}");
        return CatalogSeeder.NewProduct(
            dbContext, name, category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature, hasExpiration: true).Id;
    }

    private async Task<JsonElement> GetDashboardAsync()
    {
        var response = await fixture.Client.GetAsync(new Uri("/api/v1/dashboard", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.Clone();
    }

    private async Task<int> ListTotalCountAsync(string url)
    {
        var response = await fixture.Client.GetAsync(new Uri(url, UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("totalCount").GetInt32();
    }
}
