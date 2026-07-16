using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Shouldly;
using VetFlow.Domain.Purchasing;

namespace VetFlow.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed partial class PurchaseListEndpointTests(ApiFixture fixture)
{
    [GeneratedRegex(@"^PUR-\d{6,}$")]
    private static partial Regex PurchaseNumberFormat();

    [Fact]
    public async Task Purchase_list_returns_the_fixed_pagination_envelope_STD_API_022()
    {
        // A search term that matches nothing isolates the envelope from other tests' rows.
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/purchase-invoices?search={Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.TryGetProperty("items", out _).ShouldBeTrue();
        body.RootElement.GetProperty("page").GetInt32().ShouldBe(1);
        body.RootElement.GetProperty("pageSize").GetInt32().ShouldBe(25);
        body.RootElement.GetProperty("totalCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Search_by_partial_supplier_name_matches_normalized_forms_BR_PUR_004()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"الإمداد{marker}", Date(1), 100m));

        // Hamza-less alef must still match (write-time normalization, STD-BE-044).
        var found = await GetItemsAsync($"search=الامداد{marker}");

        found.ShouldContain(item => item.GetProperty("supplierName").GetString() == $"الإمداد{marker}");
    }

    [Fact]
    public async Task Search_by_system_number_matches_the_exact_value_BR_PUR_004()
    {
        var marker = Marker();
        string number = string.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var invoice = await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد رقم {marker}", Date(1), 100m);
            number = invoice.Number;
        });

        var byNumber = await GetItemsAsync($"search={number}");

        byNumber.ShouldContain(item => item.GetProperty("number").GetString() == number);
    }

    [Fact]
    public async Task Search_by_supplier_reference_returns_the_invoice_BR_PUR_004()
    {
        var marker = Marker();
        var reference = $"REF-{marker}";
        await fixture.SeedAsync(async dbContext =>
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد مرجع {marker}", Date(1), 100m, supplierReference: reference));

        var found = await GetItemsAsync($"search={reference}");

        found.ShouldContain(item => item.GetProperty("supplierInvoiceReference").GetString() == reference);
    }

    [Fact]
    public async Task Notes_are_not_searchable_BR_PUR_004()
    {
        var marker = Marker();
        var noteToken = $"سرية{marker}";
        await fixture.SeedAsync(async dbContext =>
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"موردملاحظات{marker}", Date(1), 100m, notes: noteToken));

        // Positive control: the invoice is findable by its supplier name…
        (await GetItemsAsync($"search=موردملاحظات{marker}")).Count.ShouldBe(1);
        // …but never by its notes text (BR-PUR-004): search excludes notes.
        (await GetItemsAsync($"search={noteToken}")).ShouldBeEmpty();
    }

    [Fact]
    public async Task Status_filter_narrows_the_list_BR_PUR_004()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
        {
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد مسودة {marker}", Date(1), 100m);
            var received = await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد مستلم {marker}", Date(2), 200m);
            var cancelled = await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد ملغى {marker}", Date(3), 300m);
            PurchasingSeeder.SetStatus(dbContext, received, PurchaseInvoiceStatus.Received);
            PurchasingSeeder.SetStatus(dbContext, cancelled, PurchaseInvoiceStatus.Cancelled);
        });

        (await GetItemsAsync($"search={marker}&status=draft")).ShouldAllBe(item =>
            item.GetProperty("status").GetString() == "draft");
        (await GetItemsAsync($"search={marker}&status=received")).ShouldAllBe(item =>
            item.GetProperty("status").GetString() == "received");
        (await GetItemsAsync($"search={marker}&status=cancelled")).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Invoice_date_range_filter_narrows_the_list_BR_PUR_004()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
        {
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد قديم {marker}", new DateOnly(2026, 1, 10), 100m);
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد وسط {marker}", new DateOnly(2026, 3, 15), 200m);
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد حديث {marker}", new DateOnly(2026, 6, 20), 300m);
        });

        var inRange = await GetItemsAsync($"search={marker}&dateFrom=2026-03-01&dateTo=2026-04-01");

        inRange.Count.ShouldBe(1);
        inRange.ShouldAllBe(item => item.GetProperty("supplierName").GetString() == $"مورد وسط {marker}");
    }

    [Fact]
    public async Task Default_order_is_the_most_recent_invoice_first_BR_PUR_004()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
        {
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد {marker} أ", new DateOnly(2026, 2, 1), 100m);
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد {marker} ب", new DateOnly(2026, 5, 1), 200m);
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد {marker} ج", new DateOnly(2026, 3, 1), 300m);
        });

        // No sort parameter — the default is invoice date descending (newest first).
        var dates = (await GetItemsAsync($"search={marker}"))
            .Select(item => item.GetProperty("invoiceDate").GetString())
            .ToList();

        dates.ShouldBe(["2026-05-01", "2026-03-01", "2026-02-01"]);
    }

    [Fact]
    public async Task Sorting_by_total_respects_direction_STD_API_023()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
        {
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد إجمالي {marker} أ", Date(1), 500m);
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد إجمالي {marker} ب", Date(2), 100m);
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد إجمالي {marker} ج", Date(3), 300m);
        });

        var ascending = (await GetItemsAsync($"search={marker}&sort=total&dir=asc"))
            .Select(item => item.GetProperty("total").GetProperty("amount").GetDecimal())
            .ToList();

        ascending.ShouldBe([100m, 300m, 500m]);
    }

    [Fact]
    public async Task Pagination_is_stable_when_invoice_dates_collide_BR_PUR_004()
    {
        // Duplicate invoice dates are common; the sort must end in a unique key so
        // paging one row at a time visits every invoice exactly once.
        var marker = Marker();
        var sharedDate = new DateOnly(2026, 4, 4);
        var seededIds = new List<Guid>();
        await fixture.SeedAsync(async dbContext =>
        {
            for (var index = 0; index < 5; index++)
            {
                var invoice = await PurchasingSeeder.NewInvoiceAsync(
                    dbContext, $"مورد تعادل {marker}", sharedDate, 100m);
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
    public async Task Pagination_pages_through_results_and_caps_page_size_STD_API_021()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
        {
            for (var index = 1; index <= 3; index++)
            {
                await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد صفحة {marker} {index}", Date(index), 100m);
            }
        });

        var page = await fixture.Client.GetAsync(
            new Uri($"/api/v1/purchase-invoices?search={marker}&page=2&pageSize=2", UriKind.Relative));
        using var body = JsonDocument.Parse(await page.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("totalCount").GetInt32().ShouldBe(3);
        body.RootElement.GetProperty("page").GetInt32().ShouldBe(2);
        body.RootElement.GetProperty("items").GetArrayLength().ShouldBe(1);

        var capped = await fixture.Client.GetAsync(
            new Uri($"/api/v1/purchase-invoices?search={marker}&pageSize=500", UriKind.Relative));
        using var cappedBody = JsonDocument.Parse(await capped.Content.ReadAsStringAsync());
        cappedBody.RootElement.GetProperty("pageSize").GetInt32().ShouldBe(100);
    }

    [Fact]
    public async Task Number_uses_the_PUR_format_and_money_uses_the_contract_shape_AC_PUR_003()
    {
        var marker = Marker();
        await fixture.SeedAsync(async dbContext =>
            await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد عقد {marker}", Date(1), 4250.75m, supplierReference: $"S-{marker}"));

        var item = (await GetItemsAsync($"search={marker}")).Single();

        // AC-PUR-003 / BR-PUR-002: PUR- prefix + at least six digits.
        PurchaseNumberFormat().IsMatch(item.GetProperty("number").GetString() ?? string.Empty).ShouldBeTrue();
        item.GetProperty("status").GetString().ShouldBe("draft");
        var total = item.GetProperty("total");
        total.GetProperty("amount").GetDecimal().ShouldBe(4250.75m);
        total.GetProperty("currency").GetString().ShouldBe("EGP");
    }

    [Fact]
    public async Task A_malformed_date_filter_is_a_validation_failure_STD_API_014()
    {
        var response = await fixture.Client.GetAsync(
            new Uri("/api/v1/purchase-invoices?dateFrom=not-a-date", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("errors").TryGetProperty("dateFrom", out _).ShouldBeTrue();
    }

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];

    private static DateOnly Date(int dayOffset) => new DateOnly(2026, 7, 1).AddDays(dayOffset);

    private async Task<List<JsonElement>> GetItemsAsync(string queryString)
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/purchase-invoices?{queryString}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. body.RootElement.GetProperty("items").EnumerateArray().Select(element => element.Clone())];
    }
}
