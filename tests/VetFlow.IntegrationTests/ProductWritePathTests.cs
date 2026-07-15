using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The create-product write path (WF-CAT-001) and the product-details read
/// (screen S2): the first command in the system, the internal-code contract
/// (DEC-CAT-026), the per-field minimum validation (AC-CAT-043), and the
/// RFC 9457 error shapes (STD-API-010/014) — all against a real PostgreSQL.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ProductWritePathTests(ApiFixture fixture)
{
    private const string InternalCodePattern = @"^PRD-\d{6,}$";
    private const string ProblemContentType = "application/problem+json";

    [Fact]
    public async Task Create_then_read_round_trips_a_complete_product_AC_CAT_001_and_REQ_CAT_008()
    {
        var (categoryId, manufacturerId, categoryName) = await SeedLookupsAsync("أدوية");

        var create = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/products", UriKind.Relative),
            ValidBody(categoryId, manufacturerId, arabicName: "أموكسيسيلين 500", boxPrice: 120m));

        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();
        created.RootElement.GetProperty("internalCode").GetString()!.ShouldMatch(InternalCodePattern);
        create.Headers.Location!.ToString().ShouldContain(id.ToString());

        var read = await fixture.Client.GetAsync(new Uri($"/api/v1/products/{id}", UriKind.Relative));
        read.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var details = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        var root = details.RootElement;
        root.GetProperty("arabicName").GetString().ShouldBe("أموكسيسيلين 500");
        root.GetProperty("categoryName").GetString().ShouldBe(categoryName);
        root.GetProperty("internalCode").GetString()!.ShouldMatch(InternalCodePattern);
        root.GetProperty("status").GetString().ShouldBe("active");
        root.GetProperty("hasSellingPrice").GetBoolean().ShouldBeTrue();

        var units = root.GetProperty("units").EnumerateArray().ToList();
        units.Count.ShouldBe(2);
        units.ShouldContain(unit => unit.GetProperty("isStorageUnit").GetBoolean());
        units.ShouldContain(unit => unit.GetProperty("isDefaultSaleUnit").GetBoolean());
        units.ShouldContain(unit => unit.GetProperty("isDefaultPurchaseUnit").GetBoolean());
    }

    [Fact]
    public async Task Internal_codes_are_unique_and_ascending_DEC_CAT_026()
    {
        var (categoryId, manufacturerId, _) = await SeedLookupsAsync("تصنيف كود");

        var first = await CreateAndReadCodeAsync(categoryId, manufacturerId, "منتج كود أول");
        var second = await CreateAndReadCodeAsync(categoryId, manufacturerId, "منتج كود ثانٍ");

        first.ShouldMatch(InternalCodePattern);
        second.ShouldMatch(InternalCodePattern);
        first.ShouldNotBe(second);
        SequenceOf(second).ShouldBeGreaterThan(SequenceOf(first));
    }

    [Fact]
    public async Task Missing_minimum_fields_are_rejected_field_by_field_AC_CAT_043()
    {
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

        var response = await fixture.Client.PostAsJsonAsync(new Uri("/api/v1/products", UriKind.Relative), body);

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
    public async Task Open_expiration_requires_its_period_AC_CAT_031()
    {
        var (categoryId, manufacturerId, _) = await SeedLookupsAsync("تصنيف صلاحية");
        var body = ValidBody(categoryId, manufacturerId, arabicName: "قطرة عين");
        var withFlag = Merge(body, new { HasOpenExpiration = true, OpenExpirationPeriodDays = (int?)null });

        var response = await fixture.Client.PostAsJsonAsync(new Uri("/api/v1/products", UriKind.Relative), withFlag);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").TryGetProperty("openExpirationPeriodDays", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Create_validation_localizes_to_english_ADR_0007()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/products", UriKind.Relative))
        {
            Content = JsonContent.Create(new { ArabicName = (string?)null }),
        };
        request.Headers.AcceptLanguage.ParseAdd("en");

        var response = await fixture.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").GetProperty("arabicName")[0].GetString()
            .ShouldBe("The Arabic name is required.");
    }

    [Fact]
    public async Task Reading_a_missing_product_is_problem_details_404_STD_API_010()
    {
        var response = await fixture.Client.GetAsync(new Uri($"/api/v1/products/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemContentType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(404);
        problem.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Possible_duplicate_needs_a_similar_name_AND_the_same_manufacturer_DEC_CAT_027()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        Guid manufacturerA = default, manufacturerB = default;
        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف تكرار {marker}");
            var manA = CatalogSeeder.NewManufacturer(dbContext, $"شركة أ {marker}");
            var manB = CatalogSeeder.NewManufacturer(dbContext, $"شركة ب {marker}");
            manufacturerA = manA.Id;
            manufacturerB = manB.Id;
            CatalogSeeder.NewProduct(dbContext, $"أموكسيسيلين حقن {marker}", category.Id, manA.Id, SeededCatalogIds.MedicineNature);
            return Task.CompletedTask;
        });

        // Same manufacturer + a hamza-less variant of the name → a possible duplicate.
        var same = await DuplicatesAsync($"اموكسيسيلين حقن {marker}", manufacturerA);
        same.ShouldContain(item => item.GetProperty("arabicName").GetString() == $"أموكسيسيلين حقن {marker}");

        // Same name, different manufacturer → not a duplicate (BR-CAT-003, DEC-CAT-027).
        var otherManufacturer = await DuplicatesAsync($"أموكسيسيلين حقن {marker}", manufacturerB);
        otherManufacturer.ShouldBeEmpty();

        // Same manufacturer, dissimilar name → not a duplicate.
        var dissimilar = await DuplicatesAsync($"شامبو مضاد للفطريات {marker}", manufacturerA);
        dissimilar.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_product_saved_without_a_price_cannot_be_sold_yet_TS_CAT_007()
    {
        var (categoryId, manufacturerId, _) = await SeedLookupsAsync("تصنيف بلا سعر");

        var create = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/products", UriKind.Relative),
            ValidBody(categoryId, manufacturerId, arabicName: "منتج بلا سعر", boxPrice: null));
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var read = await fixture.Client.GetAsync(new Uri($"/api/v1/products/{id}", UriKind.Relative));
        using var details = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        details.RootElement.GetProperty("hasSellingPrice").GetBoolean().ShouldBeFalse();
    }

    private static long SequenceOf(string internalCode) =>
        long.Parse(internalCode["PRD-".Length..], System.Globalization.CultureInfo.InvariantCulture);

    private async Task<string> CreateAndReadCodeAsync(Guid categoryId, Guid manufacturerId, string arabicName)
    {
        var response = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/products", UriKind.Relative),
            ValidBody(categoryId, manufacturerId, arabicName, boxPrice: 10m));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("internalCode").GetString()!;
    }

    private async Task<List<JsonElement>> DuplicatesAsync(string arabicName, Guid manufacturerId)
    {
        var response = await fixture.Client.GetAsync(new Uri(
            $"/api/v1/products/possible-duplicates?arabicName={Uri.EscapeDataString(arabicName)}&manufacturerId={manufacturerId}",
            UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. body.RootElement.GetProperty("items").EnumerateArray().Select(element => element.Clone())];
    }

    private async Task<(Guid CategoryId, Guid ManufacturerId, string CategoryName)> SeedLookupsAsync(string categoryName)
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var fullCategoryName = $"{categoryName} {marker}";
        Guid categoryId = default, manufacturerId = default;
        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, fullCategoryName);
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"شركة {marker}");
            categoryId = category.Id;
            manufacturerId = manufacturer.Id;
            return Task.CompletedTask;
        });

        return (categoryId, manufacturerId, fullCategoryName);
    }

    private static object ValidBody(Guid categoryId, Guid manufacturerId, string arabicName, decimal? boxPrice = null) => new
    {
        ArabicName = arabicName,
        CategoryId = categoryId,
        ManufacturerId = manufacturerId,
        NatureId = SeededCatalogIds.MedicineNature,
        Units = new object[]
        {
            new
            {
                UnitId = SeededCatalogIds.CartonUnit,
                Position = 0,
                QuantityInNextUnit = 10m,
                IsPurchaseUnit = true,
                IsSaleUnit = false,
            },
            new
            {
                UnitId = SeededCatalogIds.BoxUnit,
                Position = 1,
                QuantityInNextUnit = (decimal?)null,
                IsPurchaseUnit = false,
                IsSaleUnit = true,
                SellingPrice = boxPrice,
            },
        },
        StorageUnitId = SeededCatalogIds.BoxUnit,
        DefaultSaleUnitId = SeededCatalogIds.BoxUnit,
        DefaultPurchaseUnitId = SeededCatalogIds.CartonUnit,
    };

    private static Dictionary<string, object?> Merge(object body, object overrides)
    {
        var merged = new Dictionary<string, object?>();
        foreach (var property in body.GetType().GetProperties())
        {
            merged[property.Name] = property.GetValue(body);
        }

        foreach (var property in overrides.GetType().GetProperties())
        {
            merged[property.Name] = property.GetValue(overrides);
        }

        return merged;
    }
}
