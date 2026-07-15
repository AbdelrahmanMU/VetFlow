using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace VetFlow.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class LookupEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task The_default_unit_set_is_seeded_REQ_CAT_016()
    {
        var unitNames = await fixture.QueryDbAsync(dbContext =>
            dbContext.Units.AsNoTracking().Select(unit => unit.Name).ToListAsync());

        foreach (var name in SeededCatalogIds.DefaultUnitNames)
        {
            unitNames.ShouldContain(name);
        }
    }

    [Fact]
    public async Task The_initial_product_natures_are_available_AC_CAT_011()
    {
        var options = await GetOptionsAsync("/api/v1/product-natures");

        foreach (var name in SeededCatalogIds.InitialNatureNames)
        {
            options.ShouldContain(option => option.GetProperty("name").GetString() == name);
        }
    }

    [Fact]
    public async Task Category_options_return_the_pagination_envelope_STD_API_022()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await fixture.SeedAsync(dbContext =>
        {
            CatalogSeeder.NewCategory(dbContext, $"تصنيف قوائم {marker}");
            return Task.CompletedTask;
        });

        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/categories?search={marker}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("totalCount").GetInt32().ShouldBe(1);
        body.RootElement.GetProperty("items")[0].GetProperty("name").GetString()
            .ShouldBe($"تصنيف قوائم {marker}");
    }

    [Fact]
    public async Task Manufacturer_search_matches_normalized_arabic_STD_BE_044()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        await fixture.SeedAsync(dbContext =>
        {
            CatalogSeeder.NewManufacturer(dbContext, $"شركة الأمل {marker}");
            return Task.CompletedTask;
        });

        // Search without the hamza still finds the manufacturer.
        var options = await GetOptionsAsync($"/api/v1/manufacturers?search=الامل {marker}");

        options.ShouldContain(option => option.GetProperty("name").GetString() == $"شركة الأمل {marker}");
    }

    private async Task<List<JsonElement>> GetOptionsAsync(string path)
    {
        var response = await fixture.Client.GetAsync(new Uri(path, UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. body.RootElement.GetProperty("items").EnumerateArray().Select(element => element.Clone())];
    }
}
