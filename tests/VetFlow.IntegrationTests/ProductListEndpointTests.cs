using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace VetFlow.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class ProductListEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Product_list_returns_the_fixed_pagination_envelope_STD_API_022()
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/products?categoryId={Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.TryGetProperty("items", out _).ShouldBeTrue();
        body.RootElement.GetProperty("page").GetInt32().ShouldBe(1);
        body.RootElement.GetProperty("pageSize").GetInt32().ShouldBe(25);
        body.RootElement.GetProperty("totalCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Search_by_partial_arabic_name_matches_normalized_forms_BR_CAT_044()
    {
        var marker = Guid.NewGuid();
        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"أدوية {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"شركة {marker}");
            CatalogSeeder.NewProduct(
                dbContext, "إيموكسيكلاف 312", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature);
            return Task.CompletedTask;
        });

        // Hamza-less alef must still match (write-time normalization, STD-BE-044).
        var found = await GetItemsAsync("search=ايموكسيكلاف");

        found.ShouldContain(item => item.GetProperty("arabicName").GetString() == "إيموكسيكلاف 312");
    }

    [Fact]
    public async Task Search_by_partial_english_name_returns_the_product_BR_CAT_044()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع {marker}");
            CatalogSeeder.NewProduct(
                dbContext, $"منتج انجليزي {marker}", category.Id, manufacturer.Id,
                SeededCatalogIds.MedicineNature, englishName: $"Amoxi-{marker}");
            return Task.CompletedTask;
        });

        var found = await GetItemsAsync($"search=amoxi-{marker}");

        found.ShouldContain(item => item.GetProperty("englishName").GetString() == $"Amoxi-{marker}");
    }

    [Fact]
    public async Task Search_by_unit_barcode_returns_the_owning_product_BR_CAT_024()
    {
        var boxBarcode = $"BC-BOX-{Guid.NewGuid():N}";
        var stripBarcode = $"BC-STRIP-{Guid.NewGuid():N}";
        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {boxBarcode}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع {boxBarcode}");
            CatalogSeeder.NewProduct(
                dbContext, $"منتج باركود {boxBarcode}", category.Id, manufacturer.Id,
                SeededCatalogIds.MedicineNature, boxBarcode: boxBarcode, stripBarcode: stripBarcode);
            return Task.CompletedTask;
        });

        // REQ-CAT-040: the barcode hits the unit, the list shows the owning product —
        // whichever packaging level was scanned.
        var byBox = await GetItemsAsync($"search={boxBarcode}");
        var byStrip = await GetItemsAsync($"search={stripBarcode}");

        byBox.ShouldContain(item => item.GetProperty("arabicName").GetString() == $"منتج باركود {boxBarcode}");
        byStrip.ShouldContain(item => item.GetProperty("arabicName").GetString() == $"منتج باركود {boxBarcode}");
    }

    [Fact]
    public async Task Each_catalog_owned_filter_narrows_the_list_BR_CAT_045()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        Guid categoryA = default, categoryB = default, manufacturerA = default, manufacturerB = default;
        await fixture.SeedAsync(dbContext =>
        {
            var catA = CatalogSeeder.NewCategory(dbContext, $"أدوية {marker}");
            var catB = CatalogSeeder.NewCategory(dbContext, $"مستلزمات {marker}");
            var manA = CatalogSeeder.NewManufacturer(dbContext, $"مصنع أ {marker}");
            var manB = CatalogSeeder.NewManufacturer(dbContext, $"مصنع ب {marker}");
            categoryA = catA.Id;
            categoryB = catB.Id;
            manufacturerA = manA.Id;
            manufacturerB = manB.Id;

            CatalogSeeder.NewProduct(
                dbContext, $"دواء ثلاجة {marker}", catA.Id, manA.Id, SeededCatalogIds.MedicineNature,
                isRefrigerated: true, hasExpiration: true, isSplittable: true, boxPrice: 60m);
            var disabled = CatalogSeeder.NewProduct(
                dbContext, $"منتج معطل {marker}", catB.Id, manB.Id, SeededCatalogIds.FoodNature);
            CatalogSeeder.MarkDisabled(dbContext, disabled);
            return Task.CompletedTask;
        });

        (await GetItemsAsync($"categoryId={categoryA}")).ShouldAllBe(item =>
            item.GetProperty("arabicName").GetString() == $"دواء ثلاجة {marker}");
        (await GetItemsAsync($"manufacturerId={manufacturerB}")).ShouldAllBe(item =>
            item.GetProperty("arabicName").GetString() == $"منتج معطل {marker}");
        (await GetItemsAsync($"categoryId={categoryA}&natureId={SeededCatalogIds.MedicineNature}")).Count.ShouldBe(1);
        (await GetItemsAsync($"categoryId={categoryB}&status=disabled")).Count.ShouldBe(1);
        (await GetItemsAsync($"categoryId={categoryB}&status=active")).Count.ShouldBe(0);
        (await GetItemsAsync($"categoryId={categoryA}&refrigerated=true")).Count.ShouldBe(1);
        (await GetItemsAsync($"categoryId={categoryB}&refrigerated=true")).Count.ShouldBe(0);
        (await GetItemsAsync($"categoryId={categoryA}&hasExpiration=true&splittable=true")).Count.ShouldBe(1);
        (await GetItemsAsync($"categoryId={categoryA}&hasSellingPrice=true")).Count.ShouldBe(1);
        (await GetItemsAsync($"categoryId={categoryB}&hasSellingPrice=false")).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Sorting_by_name_respects_direction_STD_API_023()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        Guid categoryId = default;
        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"ترتيب {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع ترتيب {marker}");
            categoryId = category.Id;
            CatalogSeeder.NewProduct(dbContext, "باء منتج", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature);
            CatalogSeeder.NewProduct(dbContext, "ألف منتج", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature);
            CatalogSeeder.NewProduct(dbContext, "جيم منتج", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature);
            return Task.CompletedTask;
        });

        var ascending = (await GetItemsAsync($"categoryId={categoryId}&sort=name&dir=asc"))
            .Select(item => item.GetProperty("arabicName").GetString()).ToList();
        var descending = (await GetItemsAsync($"categoryId={categoryId}&sort=name&dir=desc"))
            .Select(item => item.GetProperty("arabicName").GetString()).ToList();

        ascending.ShouldBe(["ألف منتج", "باء منتج", "جيم منتج"]);
        descending.ShouldBe(["جيم منتج", "باء منتج", "ألف منتج"]);
    }

    [Fact]
    public async Task Sorting_by_price_puts_unpriced_products_last_STD_API_023()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        Guid categoryId = default;
        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"أسعار {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع أسعار {marker}");
            categoryId = category.Id;
            CatalogSeeder.NewProduct(dbContext, "غالٍ", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature, boxPrice: 200m);
            CatalogSeeder.NewProduct(dbContext, "بلا سعر", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature);
            CatalogSeeder.NewProduct(dbContext, "رخيص", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature, boxPrice: 10m);
            return Task.CompletedTask;
        });

        var names = (await GetItemsAsync($"categoryId={categoryId}&sort=price&dir=asc"))
            .Select(item => item.GetProperty("arabicName").GetString()).ToList();

        names.ShouldBe(["رخيص", "غالٍ", "بلا سعر"]);
    }

    [Fact]
    public async Task Pagination_pages_through_results_and_caps_page_size_STD_API_021()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        Guid categoryId = default;
        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"صفحات {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع صفحات {marker}");
            categoryId = category.Id;
            for (var index = 1; index <= 3; index++)
            {
                CatalogSeeder.NewProduct(
                    dbContext, $"منتج {index} {marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature);
            }

            return Task.CompletedTask;
        });

        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/products?categoryId={categoryId}&page=2&pageSize=2", UriKind.Relative));
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("totalCount").GetInt32().ShouldBe(3);
        body.RootElement.GetProperty("page").GetInt32().ShouldBe(2);
        body.RootElement.GetProperty("items").GetArrayLength().ShouldBe(1);

        var capped = await fixture.Client.GetAsync(
            new Uri($"/api/v1/products?categoryId={categoryId}&pageSize=500", UriKind.Relative));
        using var cappedBody = JsonDocument.Parse(await capped.Content.ReadAsStringAsync());
        cappedBody.RootElement.GetProperty("pageSize").GetInt32().ShouldBe(100);
    }

    [Fact]
    public async Task Pagination_is_stable_when_product_names_collide_BR_CAT_042()
    {
        // Regression (R3): duplicate product names are allowed (BR-CAT-042 / DEC-CAT-018),
        // so the sort must end in a unique key. Paging one row at a time must visit every
        // product exactly once — a non-unique ORDER BY could skip or repeat tied rows.
        var marker = Guid.NewGuid().ToString("N")[..8];
        var seededIds = new List<Guid>();
        Guid categoryId = default;
        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تكرار {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع تكرار {marker}");
            categoryId = category.Id;
            for (var index = 0; index < 5; index++)
            {
                // Identical Arabic name on every product — the exact collision R3 guards against.
                var product = CatalogSeeder.NewProduct(
                    dbContext, $"منتج مكرر {marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature);
                seededIds.Add(product.Id);
            }

            return Task.CompletedTask;
        });

        var pagedIds = new List<Guid>();
        for (var page = 1; page <= 5; page++)
        {
            var items = await GetItemsAsync($"categoryId={categoryId}&pageSize=1&page={page}");
            items.Count.ShouldBe(1);
            pagedIds.Add(items[0].GetProperty("id").GetGuid());
        }

        // Every seeded product appears exactly once across the pages — none skipped, none repeated.
        pagedIds.Distinct().Count().ShouldBe(5);
        pagedIds.OrderBy(id => id).ShouldBe(seededIds.OrderBy(id => id));
    }

    [Fact]
    public async Task Money_and_status_use_the_contract_shapes_STD_API_031()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        Guid categoryId = default;
        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"عملة {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنع عملة {marker}");
            categoryId = category.Id;
            CatalogSeeder.NewProduct(
                dbContext, $"منتج عملة {marker}", category.Id, manufacturer.Id,
                SeededCatalogIds.MedicineNature, boxPrice: 42.5m);
            return Task.CompletedTask;
        });

        var item = (await GetItemsAsync($"categoryId={categoryId}")).Single();

        item.GetProperty("status").GetString().ShouldBe("active");
        var price = item.GetProperty("defaultSaleUnitPrice");
        price.GetProperty("amount").GetDecimal().ShouldBe(42.5m);
        price.GetProperty("currency").GetString().ShouldBe("EGP");
        item.GetProperty("defaultSaleUnitName").GetString().ShouldBe("علبة");
        item.GetProperty("storageUnitName").GetString().ShouldBe("شريط");
    }

    private async Task<List<JsonElement>> GetItemsAsync(string queryString)
    {
        var response = await fixture.Client.GetAsync(new Uri($"/api/v1/products?{queryString}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. body.RootElement.GetProperty("items").EnumerateArray().Select(element => element.Clone())];
    }
}
