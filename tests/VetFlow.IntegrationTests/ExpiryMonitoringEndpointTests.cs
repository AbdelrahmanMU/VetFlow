using System.Net;
using System.Text.Json;
using Shouldly;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The expiry monitoring read path (REQ-INV-004) — GET /api/v1/inventory/expiry. A read-only,
/// clinic-wide view over the write-kernel InventoryBatch rows joined to the Catalog product and
/// stock unit: only active batches with a real expiry appear (BR-INV-033, DEC-INV-014), the four
/// frozen fields (BR-INV-034), the expired/expiring-soon filters over the 30-day horizon
/// (BR-INV-036), search/category (BR-INV-035), the deterministic expiry-ascending order
/// (BR-INV-037), and the read-only surface with no alerts (BR-INV-032). Expiry is computed from
/// ExpiryDate at query time — nothing is cached (DEC-INV-018).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ExpiryMonitoringEndpointTests(ApiFixture fixture)
{
    private const string SeededStockUnitName = "شريط";

    [Fact]
    public async Task Lists_active_batches_with_expiry_across_products_with_the_four_fields_BR_INV_034()
    {
        var marker = Marker();
        Guid productId = Guid.Empty;
        await fixture.SeedAsync(dbContext =>
        {
            productId = NewProduct(dbContext, $"دواء{marker}");
            InventorySeeder.AddBatch(dbContext, productId, 24m, Today().AddDays(10));
            return Task.CompletedTask;
        });

        var row = (await GetItemsAsync($"search={marker}")).Single();

        row.GetProperty("productId").GetGuid().ShouldBe(productId);
        row.GetProperty("productName").GetString().ShouldBe($"دواء{marker}");
        row.GetProperty("remainingQuantity").GetDecimal().ShouldBe(24m);
        row.GetProperty("stockUnitName").GetString().ShouldBe(SeededStockUnitName);
        row.GetProperty("expiryDate").GetString().ShouldBe(Today().AddDays(10).ToString("yyyy-MM-dd"));
        row.TryGetProperty("batchId", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Excludes_depleted_and_no_expiry_batches_DEC_INV_014()
    {
        var marker = Marker();
        await fixture.SeedAsync(dbContext =>
        {
            var product = NewProduct(dbContext, $"دواء{marker}");
            InventorySeeder.AddBatch(dbContext, product, 10m, Today().AddDays(10));  // shown
            InventorySeeder.AddBatch(dbContext, product, 10m, (DateOnly?)null);       // no expiry → excluded
            var depleted = InventorySeeder.AddBatch(dbContext, product, 10m, Today().AddDays(5)); // depleted → excluded
            InventorySeeder.MarkDepleted(dbContext, depleted);
            return Task.CompletedTask;
        });

        var expiries = (await GetItemsAsync($"search={marker}"))
            .Select(row => row.GetProperty("expiryDate").GetString())
            .ToList();

        expiries.ShouldBe([Today().AddDays(10).ToString("yyyy-MM-dd")]);
    }

    [Fact]
    public async Task Expired_filter_selects_past_expiry_only_BR_INV_036()
    {
        var marker = Marker();
        await fixture.SeedAsync(dbContext =>
        {
            var product = NewProduct(dbContext, $"دواء{marker}");
            InventorySeeder.AddBatch(dbContext, product, 10m, Today().AddDays(-1)); // expired
            InventorySeeder.AddBatch(dbContext, product, 10m, Today().AddDays(10)); // not expired
            return Task.CompletedTask;
        });

        (await GetItemsAsync($"search={marker}&expired=true"))
            .Select(row => row.GetProperty("expiryDate").GetString())
            .ShouldBe([Today().AddDays(-1).ToString("yyyy-MM-dd")]);
    }

    [Fact]
    public async Task Expiring_soon_filter_respects_the_30_day_boundary_BR_INV_036()
    {
        var marker = Marker();
        await fixture.SeedAsync(dbContext =>
        {
            var product = NewProduct(dbContext, $"دواء{marker}");
            InventorySeeder.AddBatch(dbContext, product, 10m, Today().AddDays(30)); // boundary → shown
            InventorySeeder.AddBatch(dbContext, product, 10m, Today().AddDays(31)); // beyond → hidden
            InventorySeeder.AddBatch(dbContext, product, 10m, Today().AddDays(-1)); // expired → hidden (not "soon")
            return Task.CompletedTask;
        });

        (await GetItemsAsync($"search={marker}&expiringSoon=true"))
            .Select(row => row.GetProperty("expiryDate").GetString())
            .ShouldBe([Today().AddDays(30).ToString("yyyy-MM-dd")]);
    }

    [Fact]
    public async Task Category_filter_narrows_clinic_wide_results_BR_INV_035()
    {
        var marker = Marker();
        Guid targetCategory = Guid.Empty;
        Guid targetProduct = Guid.Empty;
        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف-{Guid.NewGuid():N}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع-{Guid.NewGuid():N}");
            targetCategory = category.Id;
            targetProduct = CatalogSeeder.NewProduct(
                dbContext, $"أول{marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature, hasExpiration: true).Id;
            InventorySeeder.AddBatch(dbContext, targetProduct, 10m, Today().AddDays(10));

            var other = NewProduct(dbContext, $"ثانٍ{marker}");
            InventorySeeder.AddBatch(dbContext, other, 10m, Today().AddDays(10));
            return Task.CompletedTask;
        });

        var found = await GetItemsAsync($"search={marker}&category={targetCategory}");

        found.Count.ShouldBe(1);
        found[0].GetProperty("productId").GetGuid().ShouldBe(targetProduct);
    }

    [Fact]
    public async Task Order_is_expiry_ascending_tie_broken_by_batch_id_BR_INV_037()
    {
        var marker = Marker();
        await fixture.SeedAsync(dbContext =>
        {
            var product = NewProduct(dbContext, $"دواء{marker}");
            var shared = Today().AddDays(15);
            InventorySeeder.AddBatch(dbContext, product, 10m, Today().AddDays(25));
            InventorySeeder.AddBatch(dbContext, product, 10m, shared);
            InventorySeeder.AddBatch(dbContext, product, 10m, shared);
            InventorySeeder.AddBatch(dbContext, product, 10m, Today().AddDays(5));
            return Task.CompletedTask;
        });

        var rows = await GetItemsAsync($"search={marker}");
        var expiries = rows.Select(row => row.GetProperty("expiryDate").GetString()).ToList();

        expiries.ShouldBe(
        [
            Today().AddDays(5).ToString("yyyy-MM-dd"),
            Today().AddDays(15).ToString("yyyy-MM-dd"),
            Today().AddDays(15).ToString("yyyy-MM-dd"),
            Today().AddDays(25).ToString("yyyy-MM-dd"),
        ]);

        // The two equal-expiry rows are tie-broken by batch id ascending — a total order.
        var tied = rows.Where(row => row.GetProperty("expiryDate").GetString() == Today().AddDays(15).ToString("yyyy-MM-dd"))
            .Select(row => row.GetProperty("batchId").GetGuid()).ToList();
        tied.ShouldBe([.. tied.OrderBy(id => id)]);
    }

    [Fact]
    public async Task Exposes_no_write_endpoint_BR_INV_032()
    {
        var response = await fixture.Client.PostAsync(
            new Uri("/api/v1/inventory/expiry", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];

    /// <summary>Today at the clinic — the same basis the server uses (BR-INV-059/060), never UTC.</summary>
    private DateOnly Today() => fixture.ClinicToday;

    private static Guid NewProduct(VetFlowDbContext dbContext, string name)
    {
        var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف-{Guid.NewGuid():N}");
        var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع-{Guid.NewGuid():N}");
        return CatalogSeeder.NewProduct(
            dbContext, name, category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature, hasExpiration: true).Id;
    }

    private async Task<List<JsonElement>> GetItemsAsync(string queryString)
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/inventory/expiry?{queryString}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. body.RootElement.GetProperty("items").EnumerateArray().Select(element => element.Clone())];
    }
}
