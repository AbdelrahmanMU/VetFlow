using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The non-audited edit write path — PUT /api/v1/products/{id} (REQ-CAT-038 first
/// clause / BR-CAT-041, REQ-CAT-003, DEC-CAT-031): the round-trip, price
/// preservation, 404 on a missing product, per-field minimum validation reused
/// from create (AC-CAT-043), and the search index refreshing on a rename — all
/// against a real PostgreSQL.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ProductUpdateEndpointTests(ApiFixture fixture)
{
    private const string ProblemContentType = "application/problem+json";

    [Fact]
    public async Task Edit_updates_basic_data_and_the_unit_profile_AC_CAT_003_and_REQ_CAT_038()
    {
        var (categoryId, manufacturerId, _) = await SeedLookupsAsync("تصنيف تعديل");
        var otherCategory = await SeedCategoryAsync("تصنيف بديل");
        var id = await CreateAsync(categoryId, manufacturerId, "منتج قبل التعديل", boxPrice: 120m);

        // Rename, flip splittable, move category, and grow the profile to three levels.
        var body = new
        {
            ArabicName = "منتج بعد التعديل",
            EnglishName = "Edited product",
            Concentration = "500",
            CategoryId = otherCategory,
            ManufacturerId = manufacturerId,
            NatureId = SeededCatalogIds.FoodNature,
            IsSplittable = true,
            Units = new object[]
            {
                new { UnitId = SeededCatalogIds.CartonUnit, Position = 0, QuantityInNextUnit = 10m, IsPurchaseUnit = true, IsSaleUnit = false },
                new { UnitId = SeededCatalogIds.BoxUnit, Position = 1, QuantityInNextUnit = 5m, IsPurchaseUnit = false, IsSaleUnit = true },
                new { UnitId = SeededCatalogIds.StripUnit, Position = 2, QuantityInNextUnit = (decimal?)null, IsPurchaseUnit = false, IsSaleUnit = true },
            },
            StorageUnitId = SeededCatalogIds.BoxUnit,
            DefaultSaleUnitId = SeededCatalogIds.BoxUnit,
            DefaultPurchaseUnitId = SeededCatalogIds.CartonUnit,
        };

        var put = await fixture.Client.PutAsJsonAsync(new Uri($"/api/v1/products/{id}", UriKind.Relative), body);
        put.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var details = await ReadDetailsAsync(id);
        var root = details.RootElement;
        root.GetProperty("arabicName").GetString().ShouldBe("منتج بعد التعديل");
        root.GetProperty("englishName").GetString().ShouldBe("Edited product");
        root.GetProperty("categoryId").GetGuid().ShouldBe(otherCategory);
        root.GetProperty("natureName").GetString().ShouldBe("غذاء");
        root.GetProperty("isSplittable").GetBoolean().ShouldBeTrue();
        root.GetProperty("units").GetArrayLength().ShouldBe(3);
    }

    [Fact]
    public async Task Edit_preserves_selling_prices_when_prices_are_not_edited_DEC_CAT_031()
    {
        var (categoryId, manufacturerId, _) = await SeedLookupsAsync("تصنيف سعر محفوظ");
        var id = await CreateAsync(categoryId, manufacturerId, "منتج له سعر", boxPrice: 200m);

        // Edit only the name; the update body carries no prices at all.
        var body = MinimalEditBody(categoryId, manufacturerId, "منتج له سعر (محرَّر)");
        var put = await fixture.Client.PutAsJsonAsync(new Uri($"/api/v1/products/{id}", UriKind.Relative), body);
        put.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        using var details = await ReadDetailsAsync(id);
        details.RootElement.GetProperty("hasSellingPrice").GetBoolean().ShouldBeTrue();
        var box = details.RootElement.GetProperty("units").EnumerateArray()
            .Single(unit => unit.GetProperty("unitId").GetGuid() == SeededCatalogIds.BoxUnit);
        box.GetProperty("sellingPrice").GetProperty("amount").GetDecimal().ShouldBe(200m);
    }

    [Fact]
    public async Task Editing_a_missing_product_is_404()
    {
        var (categoryId, manufacturerId, _) = await SeedLookupsAsync("تصنيف مفقود");
        var body = MinimalEditBody(categoryId, manufacturerId, "منتج غير موجود");

        var response = await fixture.Client.PutAsJsonAsync(
            new Uri($"/api/v1/products/{Guid.NewGuid()}", UriKind.Relative), body);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Edit_rejects_missing_minimum_fields_field_by_field_AC_CAT_043()
    {
        var (categoryId, manufacturerId, _) = await SeedLookupsAsync("تصنيف تحقق");
        var id = await CreateAsync(categoryId, manufacturerId, "منتج سيُفرَّغ", boxPrice: 10m);

        var body = new
        {
            ArabicName = (string?)null,
            CategoryId = Guid.Empty,
            ManufacturerId = Guid.Empty,
            NatureId = Guid.Empty,
            Units = Array.Empty<object>(),
            StorageUnitId = Guid.Empty,
            DefaultSaleUnitId = Guid.Empty,
            DefaultPurchaseUnitId = Guid.Empty,
        };

        var response = await fixture.Client.PutAsJsonAsync(new Uri($"/api/v1/products/{id}", UriKind.Relative), body);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemContentType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errorCode").GetString().ShouldBe("VTF-VAL-001");
        var errors = problem.RootElement.GetProperty("errors");
        foreach (var field in new[] { "arabicName", "categoryId", "manufacturerId", "natureId", "units", "storageUnitId" })
        {
            errors.TryGetProperty(field, out _).ShouldBeTrue($"expected a field error for {field}");
        }
    }

    [Fact]
    public async Task Renaming_refreshes_the_search_index_BR_CAT_044()
    {
        var (categoryId, manufacturerId, _) = await SeedLookupsAsync("تصنيف بحث");
        var marker = Guid.NewGuid().ToString("N")[..8];
        var id = await CreateAsync(categoryId, manufacturerId, $"سيفازولين {marker}", boxPrice: 10m);

        var newName = $"سيفترياكسون {marker}";
        var body = MinimalEditBody(categoryId, manufacturerId, newName);
        var put = await fixture.Client.PutAsJsonAsync(new Uri($"/api/v1/products/{id}", UriKind.Relative), body);
        put.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // The interceptor must refresh the normalized search column on update.
        (await SearchIdsAsync($"سيفترياكسون {marker}")).ShouldContain(id);
        (await SearchIdsAsync($"سيفازولين {marker}")).ShouldNotContain(id);
    }

    [Fact]
    public async Task Details_expose_classification_ids_for_edit_prefill_DEC_CAT_031()
    {
        var (categoryId, manufacturerId, _) = await SeedLookupsAsync("تصنيف معرّفات");
        var id = await CreateAsync(categoryId, manufacturerId, "منتج للتعبئة المسبقة", boxPrice: 10m);

        using var details = await ReadDetailsAsync(id);
        var root = details.RootElement;
        root.GetProperty("categoryId").GetGuid().ShouldBe(categoryId);
        root.GetProperty("manufacturerId").GetGuid().ShouldBe(manufacturerId);
        root.GetProperty("natureId").GetGuid().ShouldBe(SeededCatalogIds.MedicineNature);
    }

    private async Task<JsonDocument> ReadDetailsAsync(Guid id)
    {
        var read = await fixture.Client.GetAsync(new Uri($"/api/v1/products/{id}", UriKind.Relative));
        read.StatusCode.ShouldBe(HttpStatusCode.OK);
        return JsonDocument.Parse(await read.Content.ReadAsStringAsync());
    }

    private async Task<List<Guid>> SearchIdsAsync(string search)
    {
        var response = await fixture.Client.GetAsync(new Uri(
            $"/api/v1/products?search={Uri.EscapeDataString(search)}&pageSize=50", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. body.RootElement.GetProperty("items").EnumerateArray()
            .Select(element => element.GetProperty("id").GetGuid())];
    }

    private async Task<Guid> CreateAsync(Guid categoryId, Guid manufacturerId, string arabicName, decimal? boxPrice)
    {
        var body = new
        {
            ArabicName = arabicName,
            CategoryId = categoryId,
            ManufacturerId = manufacturerId,
            NatureId = SeededCatalogIds.MedicineNature,
            Units = new object[]
            {
                new { UnitId = SeededCatalogIds.CartonUnit, Position = 0, QuantityInNextUnit = 10m, IsPurchaseUnit = true, IsSaleUnit = false },
                new { UnitId = SeededCatalogIds.BoxUnit, Position = 1, QuantityInNextUnit = (decimal?)null, IsPurchaseUnit = false, IsSaleUnit = true, SellingPrice = boxPrice },
            },
            StorageUnitId = SeededCatalogIds.BoxUnit,
            DefaultSaleUnitId = SeededCatalogIds.BoxUnit,
            DefaultPurchaseUnitId = SeededCatalogIds.CartonUnit,
        };

        var response = await fixture.Client.PostAsJsonAsync(new Uri("/api/v1/products", UriKind.Relative), body);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return created.RootElement.GetProperty("id").GetGuid();
    }

    private static object MinimalEditBody(Guid categoryId, Guid manufacturerId, string arabicName) => new
    {
        ArabicName = arabicName,
        CategoryId = categoryId,
        ManufacturerId = manufacturerId,
        NatureId = SeededCatalogIds.MedicineNature,
        Units = new object[]
        {
            new { UnitId = SeededCatalogIds.CartonUnit, Position = 0, QuantityInNextUnit = 10m, IsPurchaseUnit = true, IsSaleUnit = false },
            new { UnitId = SeededCatalogIds.BoxUnit, Position = 1, QuantityInNextUnit = (decimal?)null, IsPurchaseUnit = false, IsSaleUnit = true },
        },
        StorageUnitId = SeededCatalogIds.BoxUnit,
        DefaultSaleUnitId = SeededCatalogIds.BoxUnit,
        DefaultPurchaseUnitId = SeededCatalogIds.CartonUnit,
    };

    private async Task<Guid> SeedCategoryAsync(string categoryName)
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        Guid categoryId = default;
        await fixture.SeedAsync(dbContext =>
        {
            categoryId = CatalogSeeder.NewCategory(dbContext, $"{categoryName} {marker}").Id;
            return Task.CompletedTask;
        });
        return categoryId;
    }

    private async Task<(Guid CategoryId, Guid ManufacturerId, string CategoryName)> SeedLookupsAsync(string categoryName)
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var fullCategoryName = $"{categoryName} {marker}";
        Guid categoryId = default, manufacturerId = default;
        await fixture.SeedAsync(dbContext =>
        {
            categoryId = CatalogSeeder.NewCategory(dbContext, fullCategoryName).Id;
            manufacturerId = CatalogSeeder.NewManufacturer(dbContext, $"شركة {marker}").Id;
            return Task.CompletedTask;
        });

        return (categoryId, manufacturerId, fullCategoryName);
    }
}
