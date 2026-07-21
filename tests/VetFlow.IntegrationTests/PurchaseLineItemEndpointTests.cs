using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VetFlow.Domain.Purchasing;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The purchase line-items write and read paths (REQ-PUR-004) — POST/DELETE
/// /api/v1/purchase-invoices/{id}/lines and GET .../lines. Verifies a line is added,
/// listed, and updates the derived total (AC-PUR-008), the invoice total is the sum of
/// its lines (TS-PUR-017), invalid input is rejected field-by-field (AC-PUR-009),
/// removing a line recalculates the total (AC-PUR-010), only a purchase unit of the
/// product is accepted (TS-PUR-020), the name snapshot survives a catalog rename
/// (AC-PUR-013/TS-PUR-024), only a draft may change (AC-PUR-012), and missing invoices
/// answer 404.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class PurchaseLineItemEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task Adding_a_line_lists_it_and_updates_the_total_AC_PUR_008()
    {
        var seed = await SeedInvoiceAndProductAsync(Marker());

        var add = await AddLineAsync(seed.InvoiceId, seed.ProductId, SeededCatalogIds.CartonUnit, 3m, 100m);
        add.StatusCode.ShouldBe(HttpStatusCode.Created);

        var lines = await GetLinesAsync(seed.InvoiceId);
        lines.GetArrayLength().ShouldBe(1);
        var line = lines[0];
        line.GetProperty("productId").GetGuid().ShouldBe(seed.ProductId);
        line.GetProperty("productName").GetString().ShouldBe(seed.ProductName);
        line.GetProperty("purchaseUnitId").GetGuid().ShouldBe(SeededCatalogIds.CartonUnit);
        line.GetProperty("purchaseUnitName").GetString().ShouldBe("كرتونة");
        line.GetProperty("quantity").GetDecimal().ShouldBe(3m);
        line.GetProperty("unitPrice").GetProperty("amount").GetDecimal().ShouldBe(100m);
        line.GetProperty("unitPrice").GetProperty("currency").GetString().ShouldBe("EGP");
        line.GetProperty("lineTotal").GetProperty("amount").GetDecimal().ShouldBe(300m);

        (await GetTotalAsync(seed.InvoiceId)).ShouldBe(300m);
    }

    [Fact]
    public async Task Two_lines_sum_into_the_invoice_total_TS_PUR_017()
    {
        var seed = await SeedInvoiceAndProductAsync(Marker());

        (await AddLineAsync(seed.InvoiceId, seed.ProductId, SeededCatalogIds.CartonUnit, 2m, 150m)).EnsureSuccessStatusCode();
        (await AddLineAsync(seed.InvoiceId, seed.ProductId, SeededCatalogIds.CartonUnit, 5m, 20m)).EnsureSuccessStatusCode();

        (await GetLinesAsync(seed.InvoiceId)).GetArrayLength().ShouldBe(2);
        (await GetTotalAsync(seed.InvoiceId)).ShouldBe(400m);
    }

    [Fact]
    public async Task A_non_positive_quantity_or_negative_price_is_rejected_field_by_field_AC_PUR_009()
    {
        var seed = await SeedInvoiceAndProductAsync(Marker());

        var badQuantity = await AddLineAsync(seed.InvoiceId, seed.ProductId, SeededCatalogIds.CartonUnit, 0m, 100m);
        badQuantity.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await FieldErrorsAsync(badQuantity)).TryGetProperty("quantity", out _).ShouldBeTrue();

        var badPrice = await AddLineAsync(seed.InvoiceId, seed.ProductId, SeededCatalogIds.CartonUnit, 1m, -1m);
        badPrice.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await FieldErrorsAsync(badPrice)).TryGetProperty("unitPrice", out _).ShouldBeTrue();

        // Nothing was added.
        (await GetLinesAsync(seed.InvoiceId)).GetArrayLength().ShouldBe(0);
        (await GetTotalAsync(seed.InvoiceId)).ShouldBe(0m);
    }

    [Fact]
    public async Task Removing_a_line_recalculates_the_total_AC_PUR_010()
    {
        var seed = await SeedInvoiceAndProductAsync(Marker());
        var firstLineId = await AddLineReturningIdAsync(seed.InvoiceId, seed.ProductId, SeededCatalogIds.CartonUnit, 2m, 150m); // 300
        var secondLineId = await AddLineReturningIdAsync(seed.InvoiceId, seed.ProductId, SeededCatalogIds.CartonUnit, 5m, 20m); // 100

        var removeFirst = await fixture.Client.DeleteAsync(LineUri(seed.InvoiceId, firstLineId));
        removeFirst.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetTotalAsync(seed.InvoiceId)).ShouldBe(100m);

        var removeSecond = await fixture.Client.DeleteAsync(LineUri(seed.InvoiceId, secondLineId));
        removeSecond.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetLinesAsync(seed.InvoiceId)).GetArrayLength().ShouldBe(0);
        (await GetTotalAsync(seed.InvoiceId)).ShouldBe(0m);
    }

    [Fact]
    public async Task The_unit_must_be_a_purchase_unit_of_the_product_TS_PUR_020()
    {
        var seed = await SeedInvoiceAndProductAsync(Marker());

        // BoxUnit is a sale unit on the seeded product, not a purchase unit.
        var response = await AddLineAsync(seed.InvoiceId, seed.ProductId, SeededCatalogIds.BoxUnit, 1m, 10m);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).ShouldBe(PurchasingErrorCodes.LineComposition);
        (await GetLinesAsync(seed.InvoiceId)).GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task An_unknown_product_is_rejected_BR_PUR_005()
    {
        var seed = await SeedInvoiceAndProductAsync(Marker());

        var response = await AddLineAsync(seed.InvoiceId, Guid.NewGuid(), SeededCatalogIds.CartonUnit, 1m, 10m);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).ShouldBe(PurchasingErrorCodes.LineComposition);
    }

    [Fact]
    public async Task A_line_keeps_its_snapshot_name_after_the_product_is_renamed_AC_PUR_013()
    {
        var seed = await SeedInvoiceAndProductAsync(Marker());
        await AddLineAsync(seed.InvoiceId, seed.ProductId, SeededCatalogIds.CartonUnit, 1m, 10m);

        await fixture.SeedAsync(async dbContext =>
        {
            var product = await dbContext.Products.FirstAsync(item => item.Id == seed.ProductId);
            dbContext.Entry(product).Property(item => item.ArabicName).CurrentValue = "اسم جديد بعد التعديل";
        });

        var lines = await GetLinesAsync(seed.InvoiceId);
        lines[0].GetProperty("productName").GetString().ShouldBe(seed.ProductName);
    }

    [Fact]
    public async Task Only_a_draft_invoice_may_change_AC_PUR_012()
    {
        var marker = Marker();
        var seed = await SeedInvoiceAndProductAsync(marker);
        Guid receivedId = Guid.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var received = await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد مستلم {marker}", new DateOnly(2026, 2, 2), 0m);
            PurchasingSeeder.SetStatus(dbContext, received, PurchaseInvoiceStatus.Received);
            receivedId = received.Id;
        });

        var response = await AddLineAsync(receivedId, seed.ProductId, SeededCatalogIds.CartonUnit, 1m, 10m);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(response)).ShouldBe(PurchasingErrorCodes.InvoiceNotDraft);
    }

    [Fact]
    public async Task An_existing_draft_with_no_lines_lists_an_empty_array_BR_PUR_005()
    {
        var seed = await SeedInvoiceAndProductAsync(Marker());

        (await GetLinesAsync(seed.InvoiceId)).GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Lines_of_a_missing_invoice_answer_not_found_REQ_PUR_004()
    {
        var missing = Guid.NewGuid();

        (await fixture.Client.GetAsync(LinesUri(missing))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await AddLineAsync(missing, Guid.NewGuid(), SeededCatalogIds.CartonUnit, 1m, 10m)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await fixture.Client.DeleteAsync(LineUri(missing, Guid.NewGuid()))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<(Guid InvoiceId, Guid ProductId, string ProductName)> SeedInvoiceAndProductAsync(string marker)
    {
        var productName = $"أموكسيسيلين {marker}";
        Guid invoiceId = Guid.Empty;
        Guid productId = Guid.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            var product = CatalogSeeder.NewProduct(
                dbContext, productName, category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature);
            var invoice = await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد {marker}", new DateOnly(2026, 5, 1), 0m);
            productId = product.Id;
            invoiceId = invoice.Id;
        });

        return (invoiceId, productId, productName);
    }

    private Task<HttpResponseMessage> AddLineAsync(
        Guid invoiceId, Guid productId, Guid purchaseUnitId, decimal quantity, decimal unitPrice) =>
        fixture.Client.PostAsJsonAsync(
            LinesUri(invoiceId),
            new { productId, purchaseUnitId, quantity, unitPrice });

    private async Task<Guid> AddLineReturningIdAsync(
        Guid invoiceId, Guid productId, Guid purchaseUnitId, decimal quantity, decimal unitPrice)
    {
        var response = await AddLineAsync(invoiceId, productId, purchaseUnitId, quantity, unitPrice);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("lineId").GetGuid();
    }

    private async Task<JsonElement> GetLinesAsync(Guid invoiceId)
    {
        var response = await fixture.Client.GetAsync(LinesUri(invoiceId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.Clone();
    }

    private async Task<decimal> GetTotalAsync(Guid invoiceId)
    {
        var response = await fixture.Client.GetAsync(new Uri($"/api/v1/purchase-invoices/{invoiceId}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("total").GetProperty("amount").GetDecimal();
    }

    private static async Task<JsonElement> FieldErrorsAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("errors").Clone();
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("errorCode").GetString();
    }

    private static Uri LinesUri(Guid invoiceId) =>
        new($"/api/v1/purchase-invoices/{invoiceId}/lines", UriKind.Relative);

    private static Uri LineUri(Guid invoiceId, Guid lineId) =>
        new($"/api/v1/purchase-invoices/{invoiceId}/lines/{lineId}", UriKind.Relative);

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];
}
