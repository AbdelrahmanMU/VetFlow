using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The manufacturer management lifecycle (module: Catalog — الشركات المصنعة) against
/// a real PostgreSQL: create (REQ-CAT-013), the normalized-name uniqueness rule and
/// its database backstop (BR-CAT-007), rename (REQ-CAT-013, BR-CAT-053), and
/// activate/deactivate (REQ-CAT-048, BR-CAT-052) — with the RFC 9457 per-field shape
/// (STD-API-014). A deliberate mirror of the category management tests; manufacturer
/// list/activation scenarios have no authored TS-CAT id, so they are named by
/// REQ/BR/AC (No Speculation).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class ManufacturerManagementTests(ApiFixture fixture)
{
    private const string ProblemContentType = "application/problem+json";

    [Fact]
    public async Task Create_returns_201_and_the_active_manufacturer_appears_in_the_list_REQ_CAT_047()
    {
        var name = $"شركة الأمل {Marker()}";

        var id = await CreateAsync(name);

        var items = await ListAsync(name);
        var created = items.ShouldHaveSingleItem();
        created.GetProperty("id").GetGuid().ShouldBe(id);
        created.GetProperty("name").GetString().ShouldBe(name);
        created.GetProperty("isActive").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task A_duplicate_name_after_normalization_is_rejected_per_field_BR_CAT_007()
    {
        var marker = Marker();
        await CreateAsync($"شركة الأمل {marker}");

        // A hamza/alef variant normalizes to the same key (BR-CAT-007).
        var response = await PostCreateAsync($"شركة الامل {marker}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemContentType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errorCode").GetString().ShouldBe("VTF-VAL-001");
        problem.RootElement.GetProperty("errors").TryGetProperty("name", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task An_empty_name_is_rejected_per_field_REQ_CAT_013()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/manufacturers", UriKind.Relative), new { Name = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").TryGetProperty("name", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Renaming_to_a_valid_name_is_saved_and_listed_REQ_CAT_013()
    {
        var id = await CreateAsync($"شركة الأمل {Marker()}");
        var newName = $"شركة النور {Marker()}";

        var rename = await fixture.Client.PutAsJsonAsync(
            new Uri($"/api/v1/manufacturers/{id}", UriKind.Relative), new { Name = newName });
        rename.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var items = await ListAsync(newName);
        items.ShouldHaveSingleItem().GetProperty("name").GetString().ShouldBe(newName);
    }

    [Fact]
    public async Task Renaming_onto_an_existing_normalized_name_is_rejected_BR_CAT_007()
    {
        var marker = Marker();
        await CreateAsync($"شركة الأمل {marker}");
        var second = await CreateAsync($"شركة النور {marker}");

        var response = await fixture.Client.PutAsJsonAsync(
            new Uri($"/api/v1/manufacturers/{second}", UriKind.Relative), new { Name = $"شركة الامل {marker}" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").TryGetProperty("name", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Renaming_a_missing_manufacturer_is_404()
    {
        var response = await fixture.Client.PutAsJsonAsync(
            new Uri($"/api/v1/manufacturers/{Guid.NewGuid()}", UriKind.Relative), new { Name = "أي اسم" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivating_then_reactivating_toggles_the_listed_state_REQ_CAT_048()
    {
        var name = $"شركة الأمل {Marker()}";
        var id = await CreateAsync(name);

        var deactivate = await fixture.Client.PostAsync(
            new Uri($"/api/v1/manufacturers/{id}/deactivate", UriKind.Relative), content: null);
        deactivate.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ListAsync(name)).ShouldHaveSingleItem().GetProperty("isActive").GetBoolean().ShouldBeFalse();

        var activate = await fixture.Client.PostAsync(
            new Uri($"/api/v1/manufacturers/{id}/activate", UriKind.Relative), content: null);
        activate.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ListAsync(name)).ShouldHaveSingleItem().GetProperty("isActive").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Deactivating_a_missing_manufacturer_is_404()
    {
        var response = await fixture.Client.PostAsync(
            new Uri($"/api/v1/manufacturers/{Guid.NewGuid()}/deactivate", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_list_sort_direction_reverses_the_order_REQ_CAT_047()
    {
        // Collation-agnostic: descending is ascending reversed, whatever the
        // database's Arabic ordering — this proves the sort whitelist + direction.
        var marker = Marker();
        await CreateAsync($"باء {marker}");
        await CreateAsync($"ألف {marker}");

        var ascending = (await ListAsync(marker, "sort=name&dir=asc"))
            .Select(item => item.GetProperty("name").GetString()).ToList();
        var descending = (await ListAsync(marker, "sort=name&dir=desc"))
            .Select(item => item.GetProperty("name").GetString()).ToList();

        ascending.Count.ShouldBe(2);
        descending.ShouldBe(Enumerable.Reverse(ascending).ToList());
    }

    [Fact]
    public async Task The_unique_name_index_rejects_a_normalized_duplicate_at_the_database_BR_CAT_007()
    {
        // Proves the backstop exists independently of the handler pre-check: two
        // manufacturers whose names normalize identically cannot both persist.
        var marker = Marker();

        await Should.ThrowAsync<DbUpdateException>(fixture.SeedAsync(dbContext =>
        {
            CatalogSeeder.NewManufacturer(dbContext, $"شركة الأمل {marker}");
            CatalogSeeder.NewManufacturer(dbContext, $"شركة الامل {marker}");
            return Task.CompletedTask;
        }));
    }

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];

    private async Task<Guid> CreateAsync(string name)
    {
        var response = await PostCreateAsync(name);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private Task<HttpResponseMessage> PostCreateAsync(string name) =>
        fixture.Client.PostAsJsonAsync(new Uri("/api/v1/manufacturers", UriKind.Relative), new { Name = name });

    private async Task<List<JsonElement>> ListAsync(string search, string? extra = null)
    {
        var query = $"search={Uri.EscapeDataString(search)}" + (extra is null ? string.Empty : $"&{extra}");
        var response = await fixture.Client.GetAsync(new Uri($"/api/v1/manufacturers?{query}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. body.RootElement.GetProperty("items").EnumerateArray().Select(element => element.Clone())];
    }
}
