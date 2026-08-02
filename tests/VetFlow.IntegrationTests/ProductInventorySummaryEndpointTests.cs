using System.Net;
using System.Text.Json;
using Shouldly;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The per-product inventory summary read path (REQ-INV-012) —
/// GET /api/v1/inventory/{productId}/summary. The four inventory facts for a single
/// product, supplied by the module that owns them: on-hand as the stored canonical value
/// (BR-INV-008), the active-batch count (BR-INV-009), the nearest expiry across those same
/// batches (BR-INV-010), and the product's stock unit (BR-CAT-020). Not found vs. "exists
/// but never received" are distinct answers (BR-INV-007, DEC-INV-003), and the surface is
/// read-only (BR-INV-006).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ProductInventorySummaryEndpointTests(ApiFixture fixture)
{
    private const string SeededStockUnitName = "شريط";

    [Fact]
    public async Task Reports_on_hand_batch_count_and_stock_unit_BR_INV_008_009()
    {
        Guid productId = Guid.Empty;
        var name = $"منتج-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            productId = NewProduct(dbContext, name);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 24m);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 10m);
            InventorySeeder.SetOnHand(dbContext, productId, 34m);
        });

        var summary = await GetAsync(productId);

        // Read from the canonical ProductOnHand row (BR-INV-008) — deliberately NOT summed
        // from the batches, which is exactly the duplication this endpoint exists to avoid.
        summary.GetProperty("onHandQuantity").GetDecimal().ShouldBe(34m);
        summary.GetProperty("batchCount").GetInt32().ShouldBe(2);
        summary.GetProperty("stockUnitName").GetString().ShouldBe(SeededStockUnitName);
        summary.GetProperty("hasInventoryRecord").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Nearest_expiry_is_the_earliest_across_ACTIVE_batches_only_BR_INV_010()
    {
        Guid productId = Guid.Empty;
        var name = $"منتج-{Marker()}";
        var far = Today().AddDays(200);
        await fixture.SeedAsync(async dbContext =>
        {
            productId = NewProduct(dbContext, name);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 5m, far);
            // Depleted: it holds the earliest date of all, and must be ignored precisely
            // because BR-INV-009/010 scope both facts to batches with remaining quantity.
            var (depleted, _) = await InventorySeeder.AddBatchWithProvenanceAsync(
                dbContext, productId, name, 3m, Today().AddDays(1));
            InventorySeeder.MarkDepleted(dbContext, depleted);
            InventorySeeder.SetOnHand(dbContext, productId, 5m);
        });

        var summary = await GetAsync(productId);

        summary.GetProperty("nearestExpiry").GetString().ShouldBe(far.ToString("yyyy-MM-dd"));
        summary.GetProperty("batchCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Nearest_expiry_is_null_when_no_active_batch_carries_one_BR_INV_010()
    {
        Guid productId = Guid.Empty;
        var name = $"منتج-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            productId = NewProduct(dbContext, name);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 7m, expiryDate: null);
            InventorySeeder.SetOnHand(dbContext, productId, 7m);
        });

        var summary = await GetAsync(productId);

        summary.GetProperty("nearestExpiry").ValueKind.ShouldBe(JsonValueKind.Null);
        summary.GetProperty("batchCount").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task A_product_never_received_is_a_zero_summary_not_a_404_BR_INV_007()
    {
        Guid productId = Guid.Empty;
        await fixture.SeedAsync(dbContext =>
        {
            productId = NewProduct(dbContext, $"بلا-مخزون-{Marker()}");
            return Task.CompletedTask;
        });

        var summary = await GetAsync(productId);

        summary.GetProperty("onHandQuantity").GetDecimal().ShouldBe(0m);
        summary.GetProperty("batchCount").GetInt32().ShouldBe(0);
        summary.GetProperty("nearestExpiry").ValueKind.ShouldBe(JsonValueKind.Null);
        // The flag is what lets the screen say «لا يوجد مخزون» instead of printing a bare 0.
        summary.GetProperty("hasInventoryRecord").GetBoolean().ShouldBeFalse();
        summary.GetProperty("stockUnitName").GetString().ShouldBe(SeededStockUnitName);
    }

    [Fact]
    public async Task A_product_that_does_not_exist_is_404()
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/inventory/{Guid.NewGuid()}/summary", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// <b>The anti-drift guard.</b> The summary and the inventory projection read the same
    /// four facts through two separate EF queries — EF cannot translate a shared helper
    /// inside an expression tree, so the predicates are stated twice by necessity. This test
    /// is what keeps them one truth: for the same product, every field must agree. If someone
    /// later changes what "active batch" means in one handler and not the other, this fails.
    /// </summary>
    [Fact]
    public async Task Summary_agrees_field_for_field_with_the_inventory_projection_BR_INV_008_009_010()
    {
        Guid productId = Guid.Empty;
        var name = $"اتساق-{Marker()}";
        await fixture.SeedAsync(async dbContext =>
        {
            productId = NewProduct(dbContext, name);
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 12m, Today().AddDays(30));
            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, 8m, Today().AddDays(90));
            var (depleted, _) = await InventorySeeder.AddBatchWithProvenanceAsync(
                dbContext, productId, name, 4m, Today().AddDays(2));
            InventorySeeder.MarkDepleted(dbContext, depleted);
            InventorySeeder.SetOnHand(dbContext, productId, 20m);
        });

        var summary = await GetAsync(productId);

        var listResponse = await fixture.Client.GetAsync(
            new Uri($"/api/v1/inventory?search={Uri.EscapeDataString(name)}", UriKind.Relative));
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var listBody = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        var row = listBody.RootElement.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("productId").GetGuid() == productId);

        summary.GetProperty("onHandQuantity").GetDecimal().ShouldBe(row.GetProperty("onHandQuantity").GetDecimal());
        summary.GetProperty("batchCount").GetInt32().ShouldBe(row.GetProperty("batchCount").GetInt32());
        summary.GetProperty("stockUnitName").GetString().ShouldBe(row.GetProperty("stockUnitName").GetString());
        summary.GetProperty("nearestExpiry").GetString().ShouldBe(row.GetProperty("nearestExpiry").GetString());
    }

    [Fact]
    public async Task The_summary_exposes_no_write_endpoint_BR_INV_006()
    {
        var response = await fixture.Client.PostAsync(
            new Uri($"/api/v1/inventory/{Guid.NewGuid()}/summary", UriKind.Relative), content: null);

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

    private async Task<JsonElement> GetAsync(Guid productId)
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/inventory/{productId}/summary", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.Clone();
    }
}
