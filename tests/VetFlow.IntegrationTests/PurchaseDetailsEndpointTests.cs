using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Shouldly;
using VetFlow.Domain.Purchasing;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The purchase-details read path — GET /api/v1/purchase-invoices/{id}
/// (REQ-PUR-002). Verifies the complete header round-trips (AC-PUR-004), a
/// missing invoice is a distinct 404 (AC-PUR-005), the optional fields survive
/// their absence (TS-PUR-010), and the non-draft states are exposed (BR-PUR-003).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed partial class PurchaseDetailsEndpointTests(ApiFixture fixture)
{
    [GeneratedRegex(@"^PUR-\d{6,}$")]
    private static partial Regex PurchaseNumberFormat();

    [Fact]
    public async Task Reading_an_invoice_returns_its_complete_header_AC_PUR_004()
    {
        var marker = Marker();
        var reference = $"S-{marker}";
        var note = $"ملاحظة {marker}";
        Guid id = Guid.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var invoice = await PurchasingSeeder.NewInvoiceAsync(
                dbContext, $"مورد تفاصيل {marker}", new DateOnly(2026, 5, 20), 4250.75m,
                supplierReference: reference, notes: note);
            id = invoice.Id;
        });

        var root = await GetInvoiceAsync(id);

        root.GetProperty("id").GetGuid().ShouldBe(id);
        PurchaseNumberFormat().IsMatch(root.GetProperty("number").GetString() ?? string.Empty).ShouldBeTrue();
        root.GetProperty("supplierName").GetString().ShouldBe($"مورد تفاصيل {marker}");
        root.GetProperty("supplierInvoiceReference").GetString().ShouldBe(reference);
        root.GetProperty("invoiceDate").GetString().ShouldBe("2026-05-20");
        root.GetProperty("status").GetString().ShouldBe("draft");
        var total = root.GetProperty("total");
        total.GetProperty("amount").GetDecimal().ShouldBe(4250.75m);
        total.GetProperty("currency").GetString().ShouldBe("EGP");
        root.GetProperty("notes").GetString().ShouldBe(note);
        root.GetProperty("createdAt").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task A_missing_invoice_answers_not_found_AC_PUR_005()
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/purchase-invoices/{Guid.NewGuid()}", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Optional_reference_and_notes_are_null_when_absent_TS_PUR_010()
    {
        var marker = Marker();
        Guid id = Guid.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var invoice = await PurchasingSeeder.NewInvoiceAsync(
                dbContext, $"مورد بلا مرجع {marker}", new DateOnly(2026, 4, 1), 100m);
            id = invoice.Id;
        });

        var root = await GetInvoiceAsync(id);

        root.GetProperty("supplierInvoiceReference").ValueKind.ShouldBe(JsonValueKind.Null);
        root.GetProperty("notes").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Received_and_cancelled_states_are_exposed_BR_PUR_003()
    {
        var marker = Marker();
        Guid receivedId = Guid.Empty;
        Guid cancelledId = Guid.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var received = await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد مستلم {marker}", new DateOnly(2026, 2, 2), 200m);
            var cancelled = await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد ملغى {marker}", new DateOnly(2026, 3, 3), 300m);
            PurchasingSeeder.SetStatus(dbContext, received, PurchaseInvoiceStatus.Received);
            PurchasingSeeder.SetStatus(dbContext, cancelled, PurchaseInvoiceStatus.Cancelled);
            receivedId = received.Id;
            cancelledId = cancelled.Id;
        });

        (await GetInvoiceAsync(receivedId)).GetProperty("status").GetString().ShouldBe("received");
        (await GetInvoiceAsync(cancelledId)).GetProperty("status").GetString().ShouldBe("cancelled");
    }

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];

    private async Task<JsonElement> GetInvoiceAsync(Guid id)
    {
        var response = await fixture.Client.GetAsync(
            new Uri($"/api/v1/purchase-invoices/{id}", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.Clone();
    }
}
