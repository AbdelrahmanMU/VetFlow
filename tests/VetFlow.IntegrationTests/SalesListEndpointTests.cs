using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Shouldly;
using VetFlow.Domain.Sales;

namespace VetFlow.IntegrationTests;

/// <summary>
/// GET /api/v1/sales-invoices — the basic sales list (REQ-SAL-005, BR-SAL-019,
/// DEC-SAL-005 owner-ruled 2026-07-31). Mirrors the purchase-list suite with
/// the sales-header differences: customer instead of supplier (and optional —
/// DEC-SAL-002), sale date, two statuses, no external reference.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed partial class SalesListEndpointTests(ApiFixture fixture)
{
    [GeneratedRegex(@"^SAL-\d{6,}$")]
    private static partial Regex SalesNumberFormat();

    [Fact]
    public async Task Sales_list_returns_the_fixed_pagination_envelope_STD_API_022()
    {
        // A search term that matches nothing isolates the envelope from other tests' rows.
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/sales-invoices?search={Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.TryGetProperty("items", out _).ShouldBeTrue();
        body.RootElement.GetProperty("page").GetInt32().ShouldBe(1);
        body.RootElement.GetProperty("pageSize").GetInt32().ShouldBe(25);
        body.RootElement.GetProperty("totalCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Search_by_partial_customer_name_matches_normalized_forms_TS_SAL_026()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
            await SalesSeeder.NewInvoiceAsync(dbContext, $"الأهرام{marker}", Date(1), 100m));

        // Hamza-less alef must still match (write-time normalization, STD-BE-044).
        var found = await GetItemsAsync($"search=الاهرام{marker}");

        found.ShouldContain(item => item.GetProperty("customerName").GetString() == $"الأهرام{marker}");
    }

    [Fact]
    public async Task Search_by_system_number_matches_the_exact_value_BR_SAL_019()
    {
        var marker = Marker();
        string number = string.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var invoice = await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل رقم {marker}", Date(1), 100m);
            number = invoice.Number;
        });

        var byNumber = await GetItemsAsync($"search={number}");

        byNumber.ShouldContain(item => item.GetProperty("number").GetString() == number);
    }

    [Fact]
    public async Task An_invoice_without_a_customer_is_reachable_by_number_but_not_by_name_TS_SAL_026()
    {
        string number = string.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var invoice = await SalesSeeder.NewInvoiceAsync(dbContext, customerName: null, Date(1), 75m);
            number = invoice.Number;
        });

        // Reachable by its exact number (BR-SAL-019)…
        var byNumber = await GetItemsAsync($"search={number}");
        byNumber.Single().GetProperty("customerName").ValueKind.ShouldBe(JsonValueKind.Null);

        // …and a name search never matches it: its normalized search text is empty,
        // and an empty needle is not a wildcard.
        (await GetItemsAsync($"search=عميل-لا-يوجد-{number}")).ShouldBeEmpty();
    }

    [Fact]
    public async Task Notes_are_not_searchable_BR_SAL_019()
    {
        var marker = Marker();
        var noteToken = $"سرية{marker}";
        await fixture.SeedAsync(async dbContext =>
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميلملاحظات{marker}", Date(1), 100m, notes: noteToken));

        // Positive control: the invoice is findable by its customer name…
        (await GetItemsAsync($"search=عميلملاحظات{marker}")).Count.ShouldBe(1);
        // …but never by its notes text (BR-SAL-019): search excludes notes.
        (await GetItemsAsync($"search={noteToken}")).ShouldBeEmpty();
    }

    [Fact]
    public async Task Status_filter_narrows_the_list_TS_SAL_027()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
        {
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل مسودة {marker}", Date(1), 100m);
            var committed = await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل مثبت {marker}", Date(2), 200m);
            SalesSeeder.SetStatus(dbContext, committed, SalesInvoiceStatus.Committed);
        });

        var drafts = await GetItemsAsync($"search={marker}&status=draft");
        drafts.Count.ShouldBe(1);
        drafts.ShouldAllBe(item => item.GetProperty("status").GetString() == "draft");

        var committedItems = await GetItemsAsync($"search={marker}&status=committed");
        committedItems.Count.ShouldBe(1);
        committedItems.ShouldAllBe(item => item.GetProperty("status").GetString() == "committed");
    }

    [Fact]
    public async Task Sale_date_range_filter_narrows_the_list_TS_SAL_027()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
        {
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل قديم {marker}", new DateOnly(2026, 1, 10), 100m);
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل وسط {marker}", new DateOnly(2026, 3, 15), 200m);
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل حديث {marker}", new DateOnly(2026, 6, 20), 300m);
        });

        var inRange = await GetItemsAsync($"search={marker}&dateFrom=2026-03-01&dateTo=2026-04-01");

        inRange.Count.ShouldBe(1);
        inRange.ShouldAllBe(item => item.GetProperty("customerName").GetString() == $"عميل وسط {marker}");
    }

    [Fact]
    public async Task Default_order_is_the_most_recent_sale_first_TS_SAL_024()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
        {
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل {marker} أ", new DateOnly(2026, 2, 1), 100m);
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل {marker} ب", new DateOnly(2026, 5, 1), 200m);
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل {marker} ج", new DateOnly(2026, 3, 1), 300m);
        });

        // No sort parameter — the default is sale date descending (newest first).
        var dates = (await GetItemsAsync($"search={marker}"))
            .Select(item => item.GetProperty("saleDate").GetString())
            .ToList();

        dates.ShouldBe(["2026-05-01", "2026-03-01", "2026-02-01"]);
    }

    [Fact]
    public async Task Sorting_by_total_respects_direction_TS_SAL_028()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
        {
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل إجمالي {marker} أ", Date(1), 500m);
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل إجمالي {marker} ب", Date(2), 100m);
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل إجمالي {marker} ج", Date(3), 300m);
        });

        var ascending = (await GetItemsAsync($"search={marker}&sort=total&dir=asc"))
            .Select(item => item.GetProperty("total").GetProperty("amount").GetDecimal())
            .ToList();

        ascending.ShouldBe([100m, 300m, 500m]);
    }

    [Fact]
    public async Task Pagination_is_stable_when_sale_dates_collide_TS_SAL_028()
    {
        // Duplicate sale dates are common; the sort must end in a unique key so
        // paging one row at a time visits every invoice exactly once.
        var marker = Marker();
        var sharedDate = new DateOnly(2026, 4, 4);
        var seededIds = new List<Guid>();
        await fixture.SeedAsync(async dbContext =>
        {
            for (var index = 0; index < 5; index++)
            {
                var invoice = await SalesSeeder.NewInvoiceAsync(
                    dbContext, $"عميل تعادل {marker}", sharedDate, 100m);
                seededIds.Add(invoice.Id);
            }
        });

        var pagedIds = new List<Guid>();
        for (var page = 1; page <= 5; page++)
        {
            var items = await GetItemsAsync($"search={marker}&pageSize=1&page={page}");
            items.Count.ShouldBe(1);
            pagedIds.Add(items[0].GetProperty("id").GetGuid());
        }

        pagedIds.Distinct().Count().ShouldBe(5);
        pagedIds.OrderBy(id => id).ShouldBe(seededIds.OrderBy(id => id));
    }

    [Fact]
    public async Task Number_uses_the_SAL_format_and_money_uses_the_contract_shape_TS_SAL_029()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
            await SalesSeeder.NewInvoiceAsync(dbContext, $"عميل عقد {marker}", Date(1), 4250.75m));

        var item = (await GetItemsAsync($"search={marker}")).Single();

        // TS-SAL-029 / BR-SAL-002: SAL- prefix + at least six digits.
        SalesNumberFormat().IsMatch(item.GetProperty("number").GetString() ?? string.Empty).ShouldBeTrue();
        item.GetProperty("status").GetString().ShouldBe("draft");
        var total = item.GetProperty("total");
        total.GetProperty("amount").GetDecimal().ShouldBe(4250.75m);
        total.GetProperty("currency").GetString().ShouldBe("EGP");
    }

    [Fact]
    public async Task A_malformed_date_filter_is_a_validation_failure_STD_API_014()
    {
        var response = await fixture.Client.GetAsync(
            new Uri("/api/v1/sales-invoices?dateFrom=not-a-date", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty("dateFrom", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task An_unknown_status_token_is_a_validation_failure_STD_API_014()
    {
        // «cancelled» exists for purchases but not for sales (BR-SAL-003): the
        // sales whitelist must reject it rather than silently ignoring it.
        var response = await fixture.Client.GetAsync(
            new Uri("/api/v1/sales-invoices?status=cancelled", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty("status", out _).ShouldBeTrue();
    }

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];

    private static DateOnly Date(int dayOffset) => new DateOnly(2026, 7, 1).AddDays(dayOffset);

    private async Task<List<JsonElement>> GetItemsAsync(string queryString)
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/sales-invoices?{queryString}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. body.RootElement.GetProperty("items").EnumerateArray().Select(element => element.Clone())];
    }
}
