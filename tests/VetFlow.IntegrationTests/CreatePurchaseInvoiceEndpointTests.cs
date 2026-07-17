using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Shouldly;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The create-purchase-invoice write path — POST /api/v1/purchase-invoices
/// (REQ-PUR-003). Verifies a valid header round-trips into a Draft invoice with a
/// generated PUR- number and a zero total (AC-PUR-006), missing required fields
/// are rejected field-by-field without creating anything (AC-PUR-007), and the
/// optional fields are accepted and normalized (TS-PUR-014).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed partial class CreatePurchaseInvoiceEndpointTests(ApiFixture fixture)
{
    [GeneratedRegex(@"^PUR-\d{6,}$")]
    private static partial Regex PurchaseNumberFormat();

    [Fact]
    public async Task Create_then_read_round_trips_a_draft_header_AC_PUR_006()
    {
        var marker = Marker();
        var body = new
        {
            supplierName = $"شركة الإنشاء {marker}",
            supplierInvoiceReference = $"S-{marker}",
            invoiceDate = "2026-07-20",
            notes = $"ملاحظة {marker}",
        };

        var create = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/purchase-invoices", UriKind.Relative), body);

        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();
        var number = created.RootElement.GetProperty("number").GetString();
        PurchaseNumberFormat().IsMatch(number ?? string.Empty).ShouldBeTrue();
        create.Headers.Location!.ToString().ShouldContain(id.ToString());

        var read = await fixture.Client.GetAsync(new Uri($"/api/v1/purchase-invoices/{id}", UriKind.Relative));
        read.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var details = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        var root = details.RootElement;
        root.GetProperty("number").GetString().ShouldBe(number);
        root.GetProperty("supplierName").GetString().ShouldBe($"شركة الإنشاء {marker}");
        root.GetProperty("supplierInvoiceReference").GetString().ShouldBe($"S-{marker}");
        root.GetProperty("invoiceDate").GetString().ShouldBe("2026-07-20");
        root.GetProperty("notes").GetString().ShouldBe($"ملاحظة {marker}");
        // Born a draft with a zero total (BR-PUR-003, DEC-PUR-001 — no line items yet).
        root.GetProperty("status").GetString().ShouldBe("draft");
        root.GetProperty("total").GetProperty("amount").GetDecimal().ShouldBe(0m);
        root.GetProperty("total").GetProperty("currency").GetString().ShouldBe("EGP");
    }

    [Fact]
    public async Task Missing_required_fields_are_rejected_field_by_field_AC_PUR_007()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/purchase-invoices", UriKind.Relative),
            new { supplierName = "", supplierInvoiceReference = (string?)null, invoiceDate = (string?)null, notes = (string?)null });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("errorCode").GetString().ShouldBe("VTF-VAL-001");
        var errors = problem.RootElement.GetProperty("errors");
        errors.TryGetProperty("supplierName", out _).ShouldBeTrue();
        errors.TryGetProperty("invoiceDate", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Optional_reference_and_notes_may_be_omitted_TS_PUR_014()
    {
        var marker = Marker();
        var create = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/purchase-invoices", UriKind.Relative),
            new { supplierName = $"مورد بلا مرجع {marker}", invoiceDate = "2026-06-01" });

        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var id = created.RootElement.GetProperty("id").GetGuid();

        var read = await fixture.Client.GetAsync(new Uri($"/api/v1/purchase-invoices/{id}", UriKind.Relative));
        using var details = JsonDocument.Parse(await read.Content.ReadAsStringAsync());
        details.RootElement.GetProperty("supplierInvoiceReference").ValueKind.ShouldBe(JsonValueKind.Null);
        details.RootElement.GetProperty("notes").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Numbers_are_unique_and_ascending_across_creates_BR_PUR_002()
    {
        var marker = Marker();
        var first = await CreateAndReadNumberAsync($"مورد تسلسل {marker} أ");
        var second = await CreateAndReadNumberAsync($"مورد تسلسل {marker} ب");

        PurchaseNumberFormat().IsMatch(first).ShouldBeTrue();
        PurchaseNumberFormat().IsMatch(second).ShouldBeTrue();
        first.ShouldNotBe(second);
        SequenceOf(second).ShouldBeGreaterThan(SequenceOf(first));
    }

    private async Task<string> CreateAndReadNumberAsync(string supplierName)
    {
        var response = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/purchase-invoices", UriKind.Relative),
            new { supplierName, invoiceDate = "2026-05-05" });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("number").GetString()!;
    }

    private static long SequenceOf(string number) =>
        long.Parse(number["PUR-".Length..], System.Globalization.CultureInfo.InvariantCulture);

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];
}
