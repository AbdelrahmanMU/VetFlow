using System.Net;
using System.Text.Json;
using Shouldly;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The batch viewer read path (REQ-INV-003) — GET /api/v1/inventory/{productId}/batches. A
/// read-only per-product view over the write-kernel InventoryBatch rows joined to the Catalog
/// stock unit and the owning Purchasing invoice: all batches (active and depleted — BR-INV-019),
/// the nine frozen fields (BR-INV-020), derived Active/Depleted status (BR-INV-021), quantities
/// and unit cost as stored (BR-INV-022), the purchase reference as the owning invoice number +
/// id (BR-INV-024), the status/expired/expiring-soon filters (BR-INV-026), whitelisted sorting
/// (BR-INV-027), the deterministic default order (BR-INV-031), the not-found vs empty states
/// (AC-INV-022), and the read-only surface (BR-INV-018).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class BatchViewerEndpointTests(ApiFixture fixture)
{
    private const string SeededStockUnitName = "شريط";

    [Fact]
    public async Task Shows_every_batch_of_the_product_with_the_header_BR_INV_019()
    {
        Guid productId = Guid.Empty;
        var name = $"منتج-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            productId = NewProduct(dbContext, name);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 24m);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 10m);
        });

        var result = await GetAsync(productId);

        result.GetProperty("productName").GetString().ShouldBe(name);
        result.GetProperty("stockUnitName").GetString().ShouldBe(SeededStockUnitName);
        Items(result).Count.ShouldBe(2);
    }

    [Fact]
    public async Task Row_exposes_the_nine_fields_including_the_purchase_reference_BR_INV_020_024()
    {
        Guid productId = Guid.Empty;
        string invoiceNumber = string.Empty;
        Guid invoiceId = Guid.Empty;
        var name = $"منتج-{Marker()}";
        var expiry = Today().AddDays(50);
        await fixture.SeedAsync(async dbContext =>
        {
            productId = NewProduct(dbContext, name);
            var (batch, invoice) = await InventorySeeder.AddBatchWithProvenanceAsync(
                dbContext, productId, name, 24m, expiry, unitCost: 100m);
            invoiceNumber = invoice.Number;
            invoiceId = invoice.Id;
            _ = batch;
        });

        var row = Items(await GetAsync(productId)).Single();

        row.GetProperty("purchaseReference").GetString().ShouldBe(invoiceNumber);
        row.GetProperty("purchaseInvoiceId").GetGuid().ShouldBe(invoiceId);
        row.GetProperty("originalQuantity").GetDecimal().ShouldBe(24m);
        row.GetProperty("remainingQuantity").GetDecimal().ShouldBe(24m);
        row.GetProperty("stockUnitName").GetString().ShouldBe(SeededStockUnitName);
        row.GetProperty("unitCostSnapshot").GetDecimal().ShouldBe(100m);
        row.GetProperty("expiryDate").GetString().ShouldBe(expiry.ToString("yyyy-MM-dd"));
        row.GetProperty("status").GetString().ShouldBe("active");
        row.TryGetProperty("batchId", out _).ShouldBeTrue();
        row.TryGetProperty("receiveDate", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Status_is_derived_active_or_depleted_BR_INV_021()
    {
        Guid productId = Guid.Empty;
        var name = $"منتج-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            productId = NewProduct(dbContext, name);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 24m);
            var (depleted, _) = await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 12m);
            InventorySeeder.MarkDepleted(dbContext, depleted);
        });

        var statuses = Items(await GetAsync(productId, "sort=remainingQuantity&dir=desc"))
            .Select(row => row.GetProperty("status").GetString())
            .ToList();

        statuses.ShouldBe(["active", "depleted"]);
    }

    [Fact]
    public async Task Status_filter_keeps_only_the_requested_status_BR_INV_026()
    {
        Guid productId = Guid.Empty;
        var name = $"منتج-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            productId = NewProduct(dbContext, name);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 24m);
            var (depleted, _) = await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 12m);
            InventorySeeder.MarkDepleted(dbContext, depleted);
        });

        Items(await GetAsync(productId, "status=depleted")).Single()
            .GetProperty("status").GetString().ShouldBe("depleted");
        Items(await GetAsync(productId, "status=active")).Single()
            .GetProperty("status").GetString().ShouldBe("active");
    }

    [Fact]
    public async Task Expired_and_expiring_soon_filters_respect_the_30_day_boundary_BR_INV_026()
    {
        Guid productId = Guid.Empty;
        var name = $"منتج-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            productId = NewProduct(dbContext, name);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 10m, Today().AddDays(-1)); // expired
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 10m, Today().AddDays(30)); // soon (boundary)
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 10m, Today().AddDays(31)); // beyond → neither
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 10m, (DateOnly?)null);     // no expiry → neither
        });

        Items(await GetAsync(productId, "expired=true"))
            .Select(row => row.GetProperty("expiryDate").GetString())
            .ShouldBe([Today().AddDays(-1).ToString("yyyy-MM-dd")]);

        Items(await GetAsync(productId, "expiringSoon=true"))
            .Select(row => row.GetProperty("expiryDate").GetString())
            .ShouldBe([Today().AddDays(30).ToString("yyyy-MM-dd")]);
    }

    [Fact]
    public async Task Default_order_is_receive_date_descending_tie_broken_by_batch_id_BR_INV_031()
    {
        Guid productId = Guid.Empty;
        var name = $"منتج-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            productId = NewProduct(dbContext, name);
            // Three batches sharing one receive instant → the batch id must break the tie, so the
            // order is deterministic (stable pagination). The instant is pinned explicitly: left to
            // the seeder's default each call stamps its own "now" and nothing ties.
            var receivedAt = new DateTimeOffset(2026, 7, 20, 9, 0, 0, TimeSpan.Zero);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 10m, receivedAt: receivedAt);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 11m, receivedAt: receivedAt);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 12m, receivedAt: receivedAt);
        });

        var firstPass = Items(await GetAsync(productId))
            .Select(row => row.GetProperty("batchId").GetGuid()).ToList();
        var secondPass = Items(await GetAsync(productId))
            .Select(row => row.GetProperty("batchId").GetGuid()).ToList();

        firstPass.ShouldBe(secondPass);
        firstPass.ShouldBe([.. firstPass.OrderBy(id => id)]);
    }

    [Fact]
    public async Task Sorting_by_expiry_puts_batches_without_expiry_last_BR_INV_027()
    {
        Guid productId = Guid.Empty;
        var name = $"منتج-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            productId = NewProduct(dbContext, name);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 10m, Today().AddDays(60));
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 10m, Today().AddDays(20));
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 10m, (DateOnly?)null);
        });

        var expiries = Items(await GetAsync(productId, "sort=expiryDate&dir=asc"))
            .Select(row => row.GetProperty("expiryDate").ValueKind == JsonValueKind.Null
                ? null
                : row.GetProperty("expiryDate").GetString())
            .ToList();

        expiries.ShouldBe([Today().AddDays(20).ToString("yyyy-MM-dd"), Today().AddDays(60).ToString("yyyy-MM-dd"), null]);
    }

    [Fact]
    public async Task An_unknown_product_is_not_found_AC_INV_022()
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/inventory/{Guid.NewGuid()}/batches", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_product_with_no_batches_is_empty_not_not_found_AC_INV_022()
    {
        Guid productId = Guid.Empty;
        await fixture.SeedAsync(dbContext =>
        {
            productId = NewProduct(dbContext, $"بلا-دفعات-{Marker()}");
            return Task.CompletedTask;
        });

        var result = await GetAsync(productId);

        result.GetProperty("batches").GetProperty("totalCount").GetInt32().ShouldBe(0);
        Items(result).ShouldBeEmpty();
    }

    [Fact]
    public async Task The_viewer_exposes_no_write_endpoint_BR_INV_018()
    {
        var response = await fixture.Client.PostAsync(
            new Uri($"/api/v1/inventory/{Guid.NewGuid()}/batches", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>Today at the clinic — the same basis the server uses (BR-INV-059/060), never UTC.</summary>
    private DateOnly Today() => fixture.ClinicToday;

    private static Guid NewProduct(Infrastructure.Persistence.VetFlowDbContext dbContext, string name)
    {
        var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف-{Guid.NewGuid():N}");
        var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع-{Guid.NewGuid():N}");
        return CatalogSeeder.NewProduct(
            dbContext, name, category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature, hasExpiration: true).Id;
    }

    private async Task<JsonElement> GetAsync(Guid productId, string queryString = "")
    {
        var suffix = queryString.Length == 0 ? string.Empty : $"?{queryString}";
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/inventory/{productId}/batches{suffix}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.Clone();
    }

    private static List<JsonElement> Items(JsonElement result) =>
        [.. result.GetProperty("batches").GetProperty("items").EnumerateArray().Select(element => element.Clone())];
}
