using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VetFlow.Domain.Inventory;
using VetFlow.Domain.Sales;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The sales draft lifecycle (REQ-SAL-001 slice 1, REQ-SAL-002 slice 2): creating a draft with its
/// generated SAL- number, adding and removing lines, the derived total, the catalog snapshots, and
/// the details/lines reads. Covers TS-SAL-001..007 and TS-SAL-012.
///
/// The seeded catalog product's unit chain is carton ×12 → box ×10 → strip (the stock unit, and the
/// smallest), so a box is 10 strips. Box and strip are the sale units.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SalesInvoiceEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Creating_a_draft_assigns_a_sequential_number_and_a_zero_total_TS_SAL_001()
    {
        var consumedBefore = await ConsumeMovementCountAsync();

        var first = await CreateInvoiceAsync();
        var second = await CreateInvoiceAsync();

        first.Number.ShouldStartWith("SAL-");
        first.Number.Length.ShouldBeGreaterThanOrEqualTo("SAL-000001".Length);
        second.Number.ShouldNotBe(first.Number);

        var details = await DetailsAsync(first.Id);
        details.GetProperty("status").GetString().ShouldBe("draft");
        details.GetProperty("total").GetProperty("amount").GetDecimal().ShouldBe(0m);
        details.GetProperty("total").GetProperty("currency").GetString().ShouldBe("EGP");
        details.GetProperty("createdAt").GetDateTimeOffset().ShouldNotBe(default);

        // A draft touches no inventory at all (BR-SAL-004/010): no stock was consumed, which the
        // ledger now records (REQ-INV-009). Measured as the *change* across these two creations
        // rather than as a global count of zero: the rule is about what creating a draft does, and
        // asserting that the whole database holds no Consume row was only incidentally true while
        // no test had ever committed a sale — the sales-return tests legitimately do (C6).
        (await ConsumeMovementCountAsync()).ShouldBe(consumedBefore);
    }

    [Fact]
    public async Task Creating_without_a_sale_date_is_rejected_but_the_customer_is_optional_TS_SAL_002()
    {
        var missingDate = await fixture.Client.PostAsJsonAsync(
            InvoicesUri, new { customerName = "أحمد", notes = (string?)null });
        missingDate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        using var problem = JsonDocument.Parse(await missingDate.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errors").EnumerateObject()
            .Select(field => field.Name)
            .ShouldContain(name => name.Equals("saleDate", StringComparison.OrdinalIgnoreCase));

        // Sale date alone succeeds — no customer, no notes (DEC-SAL-002).
        var dateOnly = await fixture.Client.PostAsJsonAsync(
            InvoicesUri, new { saleDate = new DateOnly(2026, 7, 30) });
        dateOnly.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await ReadCreatedAsync(dateOnly);
        (await DetailsAsync(created.Id)).GetProperty("customerName").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task The_default_sale_unit_is_usable_and_a_non_sale_unit_is_rejected_TS_SAL_003()
    {
        var productId = await SeedProductAsync(boxPrice: 50m);
        var invoice = await CreateInvoiceAsync();

        // The product's default sale unit (box) is accepted.
        (await AddLineResponseAsync(invoice.Id, productId, SeededCatalogIds.BoxUnit, 1m))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        // The carton is a purchase unit, not a sale unit — rejected (BR-SAL-004).
        var notASaleUnit = await AddLineResponseAsync(invoice.Id, productId, SeededCatalogIds.CartonUnit, 1m);
        notASaleUnit.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(notASaleUnit)).ShouldBe(SalesErrorCodes.LineComposition);

        (await LinesAsync(invoice.Id)).GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task The_total_follows_the_lines_on_add_and_remove_TS_SAL_004()
    {
        var productId = await SeedProductAsync(boxPrice: 50m, stripPrice: 30m);
        var invoice = await CreateInvoiceAsync();

        await AddLineAsync(invoice.Id, productId, SeededCatalogIds.BoxUnit, 2m);   // 2 × 50 = 100
        var second = await AddLineAsync(invoice.Id, productId, SeededCatalogIds.StripUnit, 1m); // 1 × 30

        (await TotalAsync(invoice.Id)).ShouldBe(130m);

        (await fixture.Client.DeleteAsync(LineUri(invoice.Id, second))).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await TotalAsync(invoice.Id)).ShouldBe(100m);

        var lines = await LinesAsync(invoice.Id);
        var remaining = lines.EnumerateArray().Single().GetProperty("id").GetGuid();
        (await fixture.Client.DeleteAsync(LineUri(invoice.Id, remaining))).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await TotalAsync(invoice.Id)).ShouldBe(0m);
    }

    [Fact]
    public async Task The_invoice_total_equals_the_sum_of_the_displayed_line_totals_TS_SAL_004()
    {
        // 2.5 × 0.05 = 0.125 — a midpoint. Half away from zero gives 0.13 per line (BR-SAL-007);
        // banker's rounding, which the rule forbids, would give 0.12 and a total of 0.24.
        var productId = await SeedProductAsync(boxPrice: 0.05m, isSplittable: true);
        var invoice = await CreateInvoiceAsync();
        await AddLineAsync(invoice.Id, productId, SeededCatalogIds.BoxUnit, 2.5m);
        await AddLineAsync(invoice.Id, productId, SeededCatalogIds.BoxUnit, 2.5m);

        var lines = await LinesAsync(invoice.Id);
        lines.EnumerateArray()
            .ShouldAllBe(line => line.GetProperty("lineTotal").GetProperty("amount").GetDecimal() == 0.13m);

        // Rounded once per line, then summed — so the header matches the displayed lines exactly.
        var sum = lines.EnumerateArray()
            .Sum(line => line.GetProperty("lineTotal").GetProperty("amount").GetDecimal());
        sum.ShouldBe(0.26m);
        (await TotalAsync(invoice.Id)).ShouldBe(sum);
    }

    [Fact]
    public async Task A_line_keeps_its_snapshots_when_the_catalog_changes_TS_SAL_005()
    {
        var productId = await SeedProductAsync(boxPrice: 50m);
        var invoice = await CreateInvoiceAsync();
        await AddLineAsync(invoice.Id, productId, SeededCatalogIds.BoxUnit, 2m);

        var before = (await LinesAsync(invoice.Id)).EnumerateArray().Single();
        var capturedName = before.GetProperty("productName").GetString();

        // Rename the product and change its sale price in the catalog.
        await fixture.SeedAsync(async dbContext =>
        {
            var product = await dbContext.Products
                .Include(item => item.Units)
                .FirstAsync(item => item.Id == productId);
            dbContext.Entry(product).Property(entity => entity.ArabicName).CurrentValue = "اسم جديد تمامًا";
            var boxUnit = product.Units.First(unit => unit.UnitId == SeededCatalogIds.BoxUnit);
            dbContext.Entry(boxUnit).Property(entity => entity.SellingPrice).CurrentValue = 70m;
        });

        var after = (await LinesAsync(invoice.Id)).EnumerateArray().Single();
        after.GetProperty("productName").GetString().ShouldBe(capturedName);
        after.GetProperty("unitPrice").GetProperty("amount").GetDecimal().ShouldBe(50m);
        after.GetProperty("lineTotal").GetProperty("amount").GetDecimal().ShouldBe(100m);
        (await TotalAsync(invoice.Id)).ShouldBe(100m);
    }

    [Fact]
    public async Task A_sale_unit_with_no_catalog_price_is_rejected_TS_SAL_006()
    {
        // The strip is a sale unit of the seeded product but carries no selling price.
        var productId = await SeedProductAsync(boxPrice: 50m);
        var invoice = await CreateInvoiceAsync();

        var response = await AddLineResponseAsync(invoice.Id, productId, SeededCatalogIds.StripUnit, 1m);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).ShouldBe(SalesErrorCodes.LineComposition);
        // No price is invented and zero is not substituted.
        (await LinesAsync(invoice.Id)).GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task A_non_splittable_product_rejects_a_fractional_quantity_TS_SAL_007()
    {
        var indivisible = await SeedProductAsync(boxPrice: 50m, isSplittable: false);
        var splittable = await SeedProductAsync(boxPrice: 50m, isSplittable: true);
        var invoice = await CreateInvoiceAsync();

        (await AddLineResponseAsync(invoice.Id, indivisible, SeededCatalogIds.BoxUnit, 2.5m))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AddLineResponseAsync(invoice.Id, indivisible, SeededCatalogIds.BoxUnit, 3m))
            .StatusCode.ShouldBe(HttpStatusCode.Created);
        (await AddLineResponseAsync(invoice.Id, splittable, SeededCatalogIds.BoxUnit, 2.5m))
            .StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task A_quantity_of_zero_or_less_is_rejected_AC_SAL_003()
    {
        var productId = await SeedProductAsync(boxPrice: 50m);
        var invoice = await CreateInvoiceAsync();

        (await AddLineResponseAsync(invoice.Id, productId, SeededCatalogIds.BoxUnit, 0m))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await AddLineResponseAsync(invoice.Id, productId, SeededCatalogIds.BoxUnit, -1m))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task The_details_screen_exposes_the_header_and_no_batch_information_AC_SAL_006()
    {
        var productId = await SeedProductAsync(boxPrice: 50m);
        var created = await CreateInvoiceAsync(customerName: "عيادة النور", notes: "ملاحظة");
        await AddLineAsync(created.Id, productId, SeededCatalogIds.BoxUnit, 1m);

        var details = await DetailsAsync(created.Id);
        details.GetProperty("number").GetString().ShouldBe(created.Number);
        details.GetProperty("customerName").GetString().ShouldBe("عيادة النور");
        details.GetProperty("saleDate").GetString().ShouldNotBeNull();
        details.GetProperty("notes").GetString().ShouldBe("ملاحظة");

        // No batch column, no allocation detail, no expiry anywhere (BR-SAL-013).
        var payload = details.GetRawText() + (await LinesAsync(created.Id)).GetRawText();
        payload.ShouldNotContain("batch", Case.Insensitive);
        payload.ShouldNotContain("expiry", Case.Insensitive);
    }

    [Fact]
    public async Task A_missing_invoice_answers_not_found_on_both_reads_AC_SAL_006()
    {
        var unknown = Guid.NewGuid();

        (await fixture.Client.GetAsync(InvoiceUri(unknown))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await fixture.Client.GetAsync(LinesUri(unknown))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Removing_a_line_that_is_not_on_the_invoice_answers_not_found_BR_SAL_004()
    {
        var invoice = await CreateInvoiceAsync();

        (await fixture.Client.DeleteAsync(LineUri(invoice.Id, Guid.NewGuid())))
            .StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<Guid> SeedProductAsync(
        decimal? boxPrice = null,
        decimal? stripPrice = null,
        bool isSplittable = false)
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var productId = Guid.Empty;
        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            var product = CatalogSeeder.NewProduct(
                dbContext, $"منتج {marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature,
                isSplittable: isSplittable, boxPrice: boxPrice, stripPrice: stripPrice);
            productId = product.Id;
            return Task.CompletedTask;
        });

        return productId;
    }

    private async Task<(Guid Id, string Number)> CreateInvoiceAsync(
        string? customerName = null,
        string? notes = null)
    {
        var response = await fixture.Client.PostAsJsonAsync(
            InvoicesUri,
            new { customerName, saleDate = new DateOnly(2026, 7, 30), notes });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await ReadCreatedAsync(response);
    }

    private static async Task<(Guid Id, string Number)> ReadCreatedAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (body.RootElement.GetProperty("id").GetGuid(), body.RootElement.GetProperty("number").GetString()!);
    }

    private Task<HttpResponseMessage> AddLineResponseAsync(Guid invoiceId, Guid productId, Guid saleUnitId, decimal quantity) =>
        fixture.Client.PostAsJsonAsync(LinesUri(invoiceId), new { productId, saleUnitId, quantity });

    private async Task<Guid> AddLineAsync(Guid invoiceId, Guid productId, Guid saleUnitId, decimal quantity)
    {
        var response = await AddLineResponseAsync(invoiceId, productId, saleUnitId, quantity);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("lineId").GetGuid();
    }

    private async Task<JsonElement> DetailsAsync(Guid invoiceId)
    {
        var response = await fixture.Client.GetAsync(InvoiceUri(invoiceId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.Clone();
    }

    private async Task<JsonElement> LinesAsync(Guid invoiceId)
    {
        var response = await fixture.Client.GetAsync(LinesUri(invoiceId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.Clone();
    }

    private async Task<decimal> TotalAsync(Guid invoiceId) =>
        (await DetailsAsync(invoiceId)).GetProperty("total").GetProperty("amount").GetDecimal();

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("errorCode").GetString();
    }

    private Task<int> ConsumeMovementCountAsync() =>
        fixture.QueryDbAsync(db => db.InventoryMovements
            .CountAsync(movement => movement.Type == InventoryMovementType.Consume));

    private static readonly Uri InvoicesUri = new("/api/v1/sales-invoices", UriKind.Relative);

    private static Uri InvoiceUri(Guid invoiceId) => new($"/api/v1/sales-invoices/{invoiceId}", UriKind.Relative);

    private static Uri LinesUri(Guid invoiceId) => new($"/api/v1/sales-invoices/{invoiceId}/lines", UriKind.Relative);

    private static Uri LineUri(Guid invoiceId, Guid lineId) =>
        new($"/api/v1/sales-invoices/{invoiceId}/lines/{lineId}", UriKind.Relative);
}
