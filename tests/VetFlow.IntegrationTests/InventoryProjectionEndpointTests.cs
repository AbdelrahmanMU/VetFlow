using System.Net;
using System.Text.Json;
using Shouldly;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The inventory projection read path (REQ-INV-002) — GET /api/v1/inventory. A
/// read-only view over the write-kernel state (ProductOnHand + InventoryBatch)
/// joined to Catalog reference data: only on-hand products appear (BR-INV-007),
/// the on-hand quantity is shown in the product's stock unit (BR-INV-008), the
/// batch count is the active-batch count (BR-INV-009), the nearest expiry is the
/// minimum active-batch expiry or "—" when none (BR-INV-010), the out-of-stock
/// (BR-INV-011) and expiring-soon 30-day (BR-INV-013) filters, search/category/
/// sort (BR-INV-014), and the read-only surface (BR-INV-006). The seeded product's
/// canonical stock unit is the strip (شريط) — the Catalog seed's storage unit.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class InventoryProjectionEndpointTests(ApiFixture fixture)
{
    private const string SeededStockUnitName = "شريط";

    [Fact]
    public async Task Projection_lists_only_products_that_have_an_on_hand_record_BR_INV_007()
    {
        var marker = Marker();
        Guid withInventory = Guid.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف{marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع{marker}");

            var received = CatalogSeeder.NewProduct(
                dbContext, $"مستلم{marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature);
            withInventory = received.Id;
            InventorySeeder.SetOnHand(dbContext, received.Id, 24m);
            InventorySeeder.AddBatch(dbContext, received.Id, 24m);

            // A product that was never received — no ProductOnHand row, so it must not appear.
            CatalogSeeder.NewProduct(
                dbContext, $"غيرمستلم{marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature);

            await Task.CompletedTask;
        });

        var rows = await GetItemsAsync($"search={marker}");

        rows.Count.ShouldBe(1);
        rows[0].GetProperty("productId").GetGuid().ShouldBe(withInventory);
    }

    [Fact]
    public async Task On_hand_is_shown_in_the_product_stock_unit_AC_INV_005()
    {
        var marker = Marker();
        await SeedProductAsync($"رصيد{marker}", onHand: 24m, 10m, 14m);

        var row = (await GetItemsAsync($"search={marker}")).Single();

        row.GetProperty("onHandQuantity").GetDecimal().ShouldBe(24m);
        row.GetProperty("stockUnitName").GetString().ShouldBe(SeededStockUnitName);
    }

    [Fact]
    public async Task Batch_count_is_the_active_batch_count_BR_INV_009()
    {
        var marker = Marker();
        // Two active batches (RemainingQuantity = quantity > 0).
        await SeedProductAsync($"دفعتان{marker}", onHand: 20m, 10m, 10m);

        var row = (await GetItemsAsync($"search={marker}")).Single();

        row.GetProperty("batchCount").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task Nearest_expiry_is_the_minimum_active_batch_expiry_BR_INV_010()
    {
        var marker = Marker();
        var near = Today().AddDays(40);
        var far = Today().AddDays(90);
        await SeedProductWithExpiriesAsync($"صلاحية{marker}", onHand: 20m, near, far);

        var row = (await GetItemsAsync($"search={marker}")).Single();

        row.GetProperty("nearestExpiry").GetString().ShouldBe(near.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public async Task A_product_with_no_expiring_batch_has_no_nearest_expiry_BR_INV_010()
    {
        var marker = Marker();
        // Batches without an expiry date (product does not require expiry).
        await SeedProductWithExpiriesAsync($"بلاصلاحية{marker}", onHand: 20m, null, null);

        var row = (await GetItemsAsync($"search={marker}")).Single();

        row.GetProperty("nearestExpiry").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Out_of_stock_filter_is_empty_in_practice_after_receipts_TS_INV_009()
    {
        var marker = Marker();
        await SeedProductAsync($"موجود{marker}", onHand: 20m, 10m, 10m);

        // Only positive-on-hand rows exist (receipts only increase) — the filter returns none.
        (await GetItemsAsync($"search={marker}&outOfStock=true")).ShouldBeEmpty();
    }

    [Fact]
    public async Task Out_of_stock_filter_selects_zero_on_hand_rows_BR_INV_011()
    {
        var marker = Marker();
        // A zero on-hand record with no active batch (forward-compatible state) — proves the
        // predicate and the left-join path (count 0, expiry null).
        await SeedProductAsync($"صفر{marker}", onHand: 0m);

        var row = (await GetItemsAsync($"search={marker}&outOfStock=true")).Single();

        row.GetProperty("onHandQuantity").GetDecimal().ShouldBe(0m);
        row.GetProperty("batchCount").GetInt32().ShouldBe(0);
        row.GetProperty("nearestExpiry").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Expiring_soon_filter_respects_the_30_day_horizon_boundary_BR_INV_013()
    {
        var marker = Marker();
        await SeedProductWithExpiriesAsync($"قرب{marker}", onHand: 20m, Today().AddDays(30)); // on the horizon → shows
        await SeedProductWithExpiriesAsync($"بعيد{marker}", onHand: 20m, Today().AddDays(31)); // beyond → hidden
        await SeedProductWithExpiriesAsync($"دائم{marker}", onHand: 20m, (DateOnly?)null);      // no expiry → hidden

        var names = (await GetItemsAsync($"search={marker}&expiringSoon=true"))
            .Select(row => row.GetProperty("productName").GetString())
            .ToList();

        names.ShouldBe([$"قرب{marker}"]);
    }

    [Fact]
    public async Task Search_by_product_name_narrows_the_list_AC_INV_009()
    {
        var marker = Marker();
        await SeedProductAsync($"باراسيتامول{marker}", onHand: 10m, 10m);
        await SeedProductAsync($"أموكسيسيلين{marker}", onHand: 10m, 10m);

        var found = await GetItemsAsync($"search=باراسيتامول{marker}");

        found.Count.ShouldBe(1);
        found[0].GetProperty("productName").GetString().ShouldBe($"باراسيتامول{marker}");
    }

    [Fact]
    public async Task Category_filter_narrows_the_list_AC_INV_009()
    {
        var marker = Marker();
        var target = await SeedProductAsync($"أول{marker}", onHand: 10m, 10m);
        await SeedProductAsync($"ثانٍ{marker}", onHand: 10m, 10m);

        var found = await GetItemsAsync($"search={marker}&category={target.CategoryId}");

        found.Count.ShouldBe(1);
        found[0].GetProperty("productId").GetGuid().ShouldBe(target.ProductId);
    }

    [Fact]
    public async Task Sorting_by_nearest_expiry_orders_ascending_with_nulls_last_AC_INV_009()
    {
        var marker = Marker();
        await SeedProductWithExpiriesAsync($"جـ{marker}", onHand: 10m, Today().AddDays(60));
        await SeedProductWithExpiriesAsync($"أ{marker}", onHand: 10m, Today().AddDays(20));
        await SeedProductWithExpiriesAsync($"ب-بلا{marker}", onHand: 10m, (DateOnly?)null);

        var expiries = (await GetItemsAsync($"search={marker}&sort=nearestExpiry&dir=asc"))
            .Select(row => row.GetProperty("nearestExpiry").ValueKind == JsonValueKind.Null
                ? null
                : row.GetProperty("nearestExpiry").GetString())
            .ToList();

        expiries.ShouldBe([Today().AddDays(20).ToString("yyyy-MM-dd"), Today().AddDays(60).ToString("yyyy-MM-dd"), null]);
    }

    [Fact]
    public async Task Projection_returns_the_fixed_pagination_envelope_STD_API_022()
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/inventory?search={Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.TryGetProperty("items", out _).ShouldBeTrue();
        body.RootElement.GetProperty("page").GetInt32().ShouldBe(1);
        body.RootElement.GetProperty("pageSize").GetInt32().ShouldBe(25);
        body.RootElement.GetProperty("totalCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task A_malformed_boolean_filter_is_a_validation_failure_STD_API_014()
    {
        var response = await fixture.Client.GetAsync(
            new Uri("/api/v1/inventory?expiringSoon=maybe", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty("expiringSoon", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Projection_never_shows_uncommitted_state_BR_INV_016()
    {
        var marker = Marker();

        await fixture.AssertInvisibleWhileUncommittedAsync(
            async dbContext =>
            {
                var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف-{Guid.NewGuid():N}");
                var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع-{Guid.NewGuid():N}");
                var product = CatalogSeeder.NewProduct(
                    dbContext, $"غيرمُثبَّت{marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature);
                InventorySeeder.SetOnHand(dbContext, product.Id, 20m);
                InventorySeeder.AddBatch(dbContext, product.Id, 20m);
                await Task.CompletedTask;
            },
            async () =>
            {
                // The write is not committed — the projection (a separate connection) must not see it.
                (await GetItemsAsync($"search={marker}")).ShouldBeEmpty();
            });

        // After rollback the state never persisted either.
        (await GetItemsAsync($"search={marker}")).ShouldBeEmpty();
    }

    [Fact]
    public async Task Inventory_exposes_no_write_endpoint_BR_INV_006()
    {
        // The projection is read-only: the resource offers no create/update/delete surface.
        var response = await fixture.Client.PostAsync(
            new Uri("/api/v1/inventory", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<ProductSeed> SeedProductAsync(string name, decimal onHand, params decimal[] batchQuantities)
    {
        Guid productId = Guid.Empty;
        Guid categoryId = Guid.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف-{Guid.NewGuid():N}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع-{Guid.NewGuid():N}");
            var product = CatalogSeeder.NewProduct(
                dbContext, name, category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature);
            productId = product.Id;
            categoryId = category.Id;

            InventorySeeder.SetOnHand(dbContext, product.Id, onHand);
            foreach (var quantity in batchQuantities)
            {
                InventorySeeder.AddBatch(dbContext, product.Id, quantity);
            }

            await Task.CompletedTask;
        });

        return new ProductSeed(productId, categoryId);
    }

    private async Task<ProductSeed> SeedProductWithExpiriesAsync(string name, decimal onHand, params DateOnly?[] expiries)
    {
        Guid productId = Guid.Empty;
        Guid categoryId = Guid.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف-{Guid.NewGuid():N}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع-{Guid.NewGuid():N}");
            var product = CatalogSeeder.NewProduct(
                dbContext, name, category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature,
                hasExpiration: expiries.Any(expiry => expiry is not null));
            productId = product.Id;
            categoryId = category.Id;

            InventorySeeder.SetOnHand(dbContext, product.Id, onHand);
            foreach (var expiry in expiries)
            {
                InventorySeeder.AddBatch(dbContext, product.Id, 10m, expiry);
            }

            await Task.CompletedTask;
        });

        return new ProductSeed(productId, categoryId);
    }

    private async Task<List<JsonElement>> GetItemsAsync(string queryString)
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/inventory?{queryString}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. body.RootElement.GetProperty("items").EnumerateArray().Select(element => element.Clone())];
    }

    private sealed record ProductSeed(Guid ProductId, Guid CategoryId);
}
