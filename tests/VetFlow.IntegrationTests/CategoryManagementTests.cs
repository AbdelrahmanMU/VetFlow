using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The category management lifecycle (module: التصنيفات) against a real PostgreSQL:
/// create (REQ-CTG-002), the normalized-name uniqueness rule and its database
/// backstop (BR-CTG-003), rename (REQ-CTG-003), and activate/deactivate
/// (REQ-CTG-004) — with the RFC 9457 per-field shape (STD-API-014).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class CategoryManagementTests(ApiFixture fixture)
{
    private const string ProblemContentType = "application/problem+json";

    [Fact]
    public async Task Create_returns_201_and_the_active_category_appears_in_the_list_TS_CTG_001()
    {
        var name = $"أدوية {Marker()}";

        var id = await CreateAsync(name);

        var items = await ListAsync(name);
        var created = items.ShouldHaveSingleItem();
        created.GetProperty("id").GetGuid().ShouldBe(id);
        created.GetProperty("name").GetString().ShouldBe(name);
        created.GetProperty("isActive").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task A_duplicate_name_after_normalization_is_rejected_per_field_TS_CTG_002()
    {
        var marker = Marker();
        await CreateAsync($"أدوية {marker}");

        // A hamza/teh-marbuta variant normalizes to the same key (BR-CTG-003).
        var response = await PostCreateAsync($"ادويه {marker}");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemContentType);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errorCode").GetString().ShouldBe("VTF-VAL-001");
        problem.RootElement.GetProperty("errors").TryGetProperty("name", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task An_empty_name_is_rejected_per_field_TS_CTG_003()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/categories", UriKind.Relative), new { Name = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").TryGetProperty("name", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Renaming_to_a_valid_name_is_saved_and_listed_TS_CTG_004()
    {
        var id = await CreateAsync($"أدوية {Marker()}");
        var newName = $"أعلاف {Marker()}";

        var rename = await fixture.Client.PutAsJsonAsync(
            new Uri($"/api/v1/categories/{id}", UriKind.Relative), new { Name = newName });
        rename.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var items = await ListAsync(newName);
        items.ShouldHaveSingleItem().GetProperty("name").GetString().ShouldBe(newName);
    }

    [Fact]
    public async Task Renaming_onto_an_existing_normalized_name_is_rejected_TS_CTG_004()
    {
        var marker = Marker();
        await CreateAsync($"أدوية {marker}");
        var second = await CreateAsync($"أعلاف {marker}");

        var response = await fixture.Client.PutAsJsonAsync(
            new Uri($"/api/v1/categories/{second}", UriKind.Relative), new { Name = $"ادويه {marker}" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").TryGetProperty("name", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Renaming_a_missing_category_is_404()
    {
        var response = await fixture.Client.PutAsJsonAsync(
            new Uri($"/api/v1/categories/{Guid.NewGuid()}", UriKind.Relative), new { Name = "أي اسم" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivating_then_reactivating_toggles_the_listed_state_TS_CTG_005()
    {
        var name = $"أدوية {Marker()}";
        var id = await CreateAsync(name);

        var deactivate = await fixture.Client.PostAsync(
            new Uri($"/api/v1/categories/{id}/deactivate", UriKind.Relative), content: null);
        deactivate.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ListAsync(name)).ShouldHaveSingleItem().GetProperty("isActive").GetBoolean().ShouldBeFalse();

        var activate = await fixture.Client.PostAsync(
            new Uri($"/api/v1/categories/{id}/activate", UriKind.Relative), content: null);
        activate.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await ListAsync(name)).ShouldHaveSingleItem().GetProperty("isActive").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Deactivating_a_missing_category_is_404()
    {
        var response = await fixture.Client.PostAsync(
            new Uri($"/api/v1/categories/{Guid.NewGuid()}/deactivate", UriKind.Relative), content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_list_sort_direction_reverses_the_order_REQ_CTG_001()
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
    public async Task The_unique_name_index_rejects_a_normalized_duplicate_at_the_database_BR_CTG_003()
    {
        // Proves the backstop exists independently of the handler pre-check: two
        // categories whose names normalize identically cannot both persist.
        var marker = Marker();

        await Should.ThrowAsync<DbUpdateException>(fixture.SeedAsync(dbContext =>
        {
            CatalogSeeder.NewCategory(dbContext, $"أدوية {marker}");
            CatalogSeeder.NewCategory(dbContext, $"ادويه {marker}");
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
        fixture.Client.PostAsJsonAsync(new Uri("/api/v1/categories", UriKind.Relative), new { Name = name });

    private async Task<List<JsonElement>> ListAsync(string search, string? extra = null)
    {
        var query = $"search={Uri.EscapeDataString(search)}" + (extra is null ? string.Empty : $"&{extra}");
        var response = await fixture.Client.GetAsync(new Uri($"/api/v1/categories?{query}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. body.RootElement.GetProperty("items").EnumerateArray().Select(element => element.Clone())];
    }
}
