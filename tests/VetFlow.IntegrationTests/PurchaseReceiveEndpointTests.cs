using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Inventory;
using VetFlow.Domain.Purchasing;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The purchase-receiving write path (REQ-PUR-005) and the Inventory write kernel (write-kernel.md)
/// — POST /api/v1/purchase-invoices/{id}/receive. Verifies the atomic effect: the invoice becomes
/// Received (AC-PUR-014), one batch per line is created with the minimal fields and the on-hand
/// quantity is incremented, both in the product's canonical stock unit — converted through the
/// Catalog unit profile (owner ruling 2026-07-22): the seeded product's chain is carton ×12 → box
/// ×10 → strip (storage), so a carton is 120 strips. Also: receiving is one-time (AC-PUR-015), an
/// empty invoice is rejected (AC-PUR-016), a received invoice is immutable (AC-PUR-017), expiry is
/// product-driven (AC-PUR-018), a rejected receive persists nothing (AC-INV-003), the same product
/// on two lines increments one on-hand row (TS-INV-001/002), and requiresExpiry is surfaced.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class PurchaseReceiveEndpointTests(ApiFixture fixture)
{
    private const decimal CartonToStrip = 120m; // 12 boxes/carton × 10 strips/box (seeded profile)

    [Fact]
    public async Task Receiving_a_draft_creates_batches_and_increments_on_hand_AC_PUR_014()
    {
        var seed = await SeedAsync(Marker());
        var lineId = await AddLineAsync(seed.InvoiceId, seed.ProductId, 2m, 100m);

        var response = await ReceiveAsync(seed.InvoiceId);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await StatusAsync(seed.InvoiceId)).ShouldBe(PurchaseInvoiceStatus.Received);

        var batches = await BatchesAsync(seed.ProductId);
        batches.Count.ShouldBe(1);
        batches[0].PurchaseLineId.ShouldBe(lineId);
        batches[0].Quantity.ShouldBe(2m * CartonToStrip);         // 240 strips (converted)
        batches[0].RemainingQuantity.ShouldBe(2m * CartonToStrip); // = quantity (BR-INV-001)
        batches[0].UnitCostSnapshot.ShouldBe(100m);
        batches[0].ExpiryDate.ShouldBeNull();                      // product does not require expiry

        (await OnHandAsync(seed.ProductId)).ShouldBe(2m * CartonToStrip);
    }

    [Fact]
    public async Task Two_lines_of_the_same_product_make_two_batches_and_one_on_hand_row_TS_INV_001()
    {
        var seed = await SeedAsync(Marker());
        await AddLineAsync(seed.InvoiceId, seed.ProductId, 1m, 100m);
        await AddLineAsync(seed.InvoiceId, seed.ProductId, 1m, 100m);

        (await ReceiveAsync(seed.InvoiceId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await BatchesAsync(seed.ProductId)).Count.ShouldBe(2);
        (await OnHandAsync(seed.ProductId)).ShouldBe(2m * CartonToStrip); // one row, incremented twice
    }

    [Fact]
    public async Task Receiving_an_empty_invoice_is_rejected_AC_PUR_016()
    {
        var seed = await SeedAsync(Marker());

        var response = await ReceiveAsync(seed.InvoiceId);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(response)).ShouldBe(PurchasingErrorCodes.InvoiceHasNoLines);
        (await StatusAsync(seed.InvoiceId)).ShouldBe(PurchaseInvoiceStatus.Draft);
    }

    [Fact]
    public async Task Receiving_twice_is_rejected_AC_PUR_015()
    {
        var seed = await SeedAsync(Marker());
        await AddLineAsync(seed.InvoiceId, seed.ProductId, 1m, 100m);
        (await ReceiveAsync(seed.InvoiceId)).EnsureSuccessStatusCode();

        var second = await ReceiveAsync(seed.InvoiceId);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(second)).ShouldBe(PurchasingErrorCodes.InvoiceNotDraft);
        (await BatchesAsync(seed.ProductId)).Count.ShouldBe(1); // no second set of batches
    }

    [Fact]
    public async Task Receiving_a_cancelled_invoice_is_rejected_AC_PUR_015()
    {
        var marker = Marker();
        var seed = await SeedAsync(marker);
        await AddLineAsync(seed.InvoiceId, seed.ProductId, 1m, 100m);
        await fixture.SeedAsync(async dbContext =>
        {
            var invoice = await dbContext.PurchaseInvoices.FirstAsync(item => item.Id == seed.InvoiceId);
            PurchasingSeeder.SetStatus(dbContext, invoice, PurchaseInvoiceStatus.Cancelled);
        });

        var response = await ReceiveAsync(seed.InvoiceId);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(response)).ShouldBe(PurchasingErrorCodes.InvoiceNotDraft);
        (await BatchesAsync(seed.ProductId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task A_received_invoice_cannot_add_or_remove_lines_AC_PUR_017()
    {
        var seed = await SeedAsync(Marker());
        var lineId = await AddLineAsync(seed.InvoiceId, seed.ProductId, 1m, 100m);
        (await ReceiveAsync(seed.InvoiceId)).EnsureSuccessStatusCode();

        var add = await fixture.Client.PostAsJsonAsync(
            LinesUri(seed.InvoiceId),
            new { productId = seed.ProductId, purchaseUnitId = SeededCatalogIds.CartonUnit, quantity = 1m, unitPrice = 10m });
        add.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var remove = await fixture.Client.DeleteAsync(
            new Uri($"/api/v1/purchase-invoices/{seed.InvoiceId}/lines/{lineId}", UriKind.Relative));
        remove.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_product_requiring_expiry_rejects_a_missing_expiry_and_persists_nothing_AC_PUR_018()
    {
        var seed = await SeedAsync(Marker(), requiresExpiry: true);
        await AddLineAsync(seed.InvoiceId, seed.ProductId, 1m, 100m);

        var response = await ReceiveAsync(seed.InvoiceId); // no expiry supplied

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).ShouldBe(PurchasingErrorCodes.ExpiryRequired);
        // Atomic reject — nothing persisted (AC-INV-003).
        (await StatusAsync(seed.InvoiceId)).ShouldBe(PurchaseInvoiceStatus.Draft);
        (await BatchesAsync(seed.ProductId)).ShouldBeEmpty();
        (await OnHandAsync(seed.ProductId)).ShouldBeNull();
    }

    [Fact]
    public async Task A_product_requiring_expiry_stores_the_supplied_expiry_AC_PUR_018()
    {
        var seed = await SeedAsync(Marker(), requiresExpiry: true);
        var lineId = await AddLineAsync(seed.InvoiceId, seed.ProductId, 1m, 100m);
        var expiry = new DateOnly(2027, 3, 1);

        (await ReceiveAsync(seed.InvoiceId, (lineId, expiry))).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var batches = await BatchesAsync(seed.ProductId);
        batches.Count.ShouldBe(1);
        batches[0].ExpiryDate.ShouldBe(expiry);
    }

    [Fact]
    public async Task A_product_not_requiring_expiry_creates_a_batch_without_expiry_AC_PUR_018()
    {
        var seed = await SeedAsync(Marker());
        var lineId = await AddLineAsync(seed.InvoiceId, seed.ProductId, 1m, 100m);

        // Even a supplied date is ignored when the product does not require expiry (DEC-PUR-009).
        (await ReceiveAsync(seed.InvoiceId, (lineId, new DateOnly(2027, 3, 1)))).EnsureSuccessStatusCode();

        (await BatchesAsync(seed.ProductId))[0].ExpiryDate.ShouldBeNull();
    }

    [Fact]
    public async Task Requires_expiry_is_surfaced_on_the_lines_read_DEC_PUR_009()
    {
        var expiryProduct = await SeedAsync(Marker(), requiresExpiry: true);
        await AddLineAsync(expiryProduct.InvoiceId, expiryProduct.ProductId, 1m, 100m);

        var plain = await SeedAsync(Marker());
        await AddLineAsync(plain.InvoiceId, plain.ProductId, 1m, 100m);

        (await LinesAsync(expiryProduct.InvoiceId))[0].GetProperty("requiresExpiry").GetBoolean().ShouldBeTrue();
        (await LinesAsync(plain.InvoiceId))[0].GetProperty("requiresExpiry").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Conversion_divides_when_the_stock_unit_is_not_the_smallest_BR_PUR_010()
    {
        // A product whose storage (stock) unit is the middle unit (box), not the smallest (strip):
        // carton ×12 → box (storage) ×10 → strip. Receiving in cartons converts × factor(carton, =120)
        // ÷ factor(box, =10) = ×12, exercising the divide branch (storage factor > 1).
        var marker = Marker();
        Guid invoiceId = Guid.Empty;
        Guid productId = Guid.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            var units = new List<ProductUnit>
            {
                new(Guid.NewGuid(), SeededCatalogIds.CartonUnit, 0, 12, isPurchaseUnit: true, isSaleUnit: false),
                new(Guid.NewGuid(), SeededCatalogIds.BoxUnit, 1, 10, isPurchaseUnit: false, isSaleUnit: true),
                new(Guid.NewGuid(), SeededCatalogIds.StripUnit, 2, null, isPurchaseUnit: false, isSaleUnit: true),
            };
            var product = new Product(
                Guid.NewGuid(), $"PRD-SEED-{Guid.NewGuid():N}", $"منتج تخزين وسطي {marker}",
                category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature,
                new ProductCapabilities(false, false, false, false, null), units,
                storageUnitId: SeededCatalogIds.BoxUnit,      // storage is the MIDDLE unit
                defaultSaleUnitId: SeededCatalogIds.BoxUnit,
                defaultPurchaseUnitId: SeededCatalogIds.CartonUnit);
            dbContext.Products.Add(product);
            var invoice = await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد {marker}", new DateOnly(2026, 5, 1), 0m);
            productId = product.Id;
            invoiceId = invoice.Id;
        });
        await AddLineAsync(invoiceId, productId, 2m, 100m); // 2 cartons

        (await ReceiveAsync(invoiceId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await BatchesAsync(productId))[0].Quantity.ShouldBe(24m); // 2 × (120 ÷ 10) = 24 boxes
        (await OnHandAsync(productId)).ShouldBe(24m);
    }

    [Fact]
    public async Task Receiving_a_missing_invoice_answers_not_found_REQ_PUR_005()
    {
        (await ReceiveAsync(Guid.NewGuid())).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<(Guid InvoiceId, Guid ProductId)> SeedAsync(string marker, bool requiresExpiry = false)
    {
        Guid invoiceId = Guid.Empty;
        Guid productId = Guid.Empty;
        await fixture.SeedAsync(async dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            var product = CatalogSeeder.NewProduct(
                dbContext, $"منتج {marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature,
                hasExpiration: requiresExpiry);
            var invoice = await PurchasingSeeder.NewInvoiceAsync(dbContext, $"مورد {marker}", new DateOnly(2026, 5, 1), 0m);
            productId = product.Id;
            invoiceId = invoice.Id;
        });

        return (invoiceId, productId);
    }

    private async Task<Guid> AddLineAsync(Guid invoiceId, Guid productId, decimal quantity, decimal unitPrice)
    {
        var response = await fixture.Client.PostAsJsonAsync(
            LinesUri(invoiceId),
            new { productId, purchaseUnitId = SeededCatalogIds.CartonUnit, quantity, unitPrice });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("lineId").GetGuid();
    }

    private Task<HttpResponseMessage> ReceiveAsync(Guid invoiceId, params (Guid LineId, DateOnly Expiry)[] expiries) =>
        fixture.Client.PostAsJsonAsync(
            new Uri($"/api/v1/purchase-invoices/{invoiceId}/receive", UriKind.Relative),
            new { lines = expiries.Select(entry => new { lineId = entry.LineId, expiryDate = entry.Expiry }).ToArray() });

    [Fact]
    public async Task Receiving_writes_one_increase_movement_to_the_ledger_per_batch_BR_INV_062()
    {
        var seed = await SeedAsync(Marker());
        var lineId = await AddLineAsync(seed.InvoiceId, seed.ProductId, 2m, 100m);

        (await ReceiveAsync(seed.InvoiceId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var movements = await MovementsAsync(seed.ProductId);
        movements.Count.ShouldBe(1);

        var movement = movements.Single();
        movement.Type.ShouldBe(InventoryMovementType.Receive);
        movement.Source.ShouldBe(InventoryMovementSource.Purchasing);
        movement.BatchId.ShouldBe((await BatchesAsync(seed.ProductId)).Single().Id);
        // Signed: receiving increases stock, in the canonical stock unit (BR-INV-064).
        movement.Quantity.ShouldBe(2m * CartonToStrip);
        movement.ReferenceId.ShouldBe(lineId);
        // A document-driven movement carries no inventory-native reason or actor (BR-INV-067).
        movement.Reason.ShouldBeNull();
        movement.ActorName.ShouldBeNull();
    }

    [Fact]
    public async Task The_ledger_records_history_and_never_owns_the_quantities_BR_INV_063()
    {
        var seed = await SeedAsync(Marker());
        await AddLineAsync(seed.InvoiceId, seed.ProductId, 1m, 100m);
        await AddLineAsync(seed.InvoiceId, seed.ProductId, 1m, 100m);

        (await ReceiveAsync(seed.InvoiceId)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Two lines → two batches → two appended rows; the ledger is never rewritten (DEC-INV-037).
        var movements = await MovementsAsync(seed.ProductId);
        movements.Count.ShouldBe(2);
        movements.Select(movement => movement.BatchId).Distinct().Count().ShouldBe(2);

        // The ledger agrees with the authoritative state — but the batches and the on-hand row are
        // the source of truth, never the ledger (BR-INV-001/002/005, BR-INV-063).
        movements.Sum(movement => movement.Quantity).ShouldBe(2m * CartonToStrip);
        (await OnHandAsync(seed.ProductId)).ShouldBe(2m * CartonToStrip);
        (await BatchesAsync(seed.ProductId)).Sum(batch => batch.RemainingQuantity)
            .ShouldBe(2m * CartonToStrip);
    }

    private Task<List<InventoryMovement>> MovementsAsync(Guid productId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryMovements
            .Where(movement => movement.ProductId == productId)
            .OrderBy(movement => movement.OccurredAt)
            .ThenBy(movement => movement.Id)
            .ToListAsync());

    private Task<List<InventoryBatch>> BatchesAsync(Guid productId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.ProductId == productId)
            .ToListAsync());

    private Task<decimal?> OnHandAsync(Guid productId) =>
        fixture.QueryDbAsync(async dbContext =>
        {
            var onHand = await dbContext.ProductOnHands.FirstOrDefaultAsync(item => item.ProductId == productId);
            return onHand?.OnHandQuantity;
        });

    private Task<PurchaseInvoiceStatus> StatusAsync(Guid invoiceId) =>
        fixture.QueryDbAsync(dbContext => dbContext.PurchaseInvoices
            .Where(invoice => invoice.Id == invoiceId)
            .Select(invoice => invoice.Status)
            .FirstAsync());

    private async Task<JsonElement> LinesAsync(Guid invoiceId)
    {
        var response = await fixture.Client.GetAsync(LinesUri(invoiceId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.Clone();
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("errorCode").GetString();
    }

    private static Uri LinesUri(Guid invoiceId) => new($"/api/v1/purchase-invoices/{invoiceId}/lines", UriKind.Relative);

    private static string Marker() => Guid.NewGuid().ToString("N")[..8];
}
