using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VetFlow.Domain.Inventory;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Write-off (REQ-INV-011) — POST /api/v1/inventory/write-offs, end to end through the real API and
/// a real PostgreSQL. Covers TS-INV-063..066.
///
/// <b>This is what closes R9.</b> Expired stock has been visible, unsaleable (DEC-INV-021) and
/// stuck inside the on-hand quantity since Sprint 7; the last test here is the proof it can finally
/// leave.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class InventoryWriteOffEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task A_write_off_lowers_the_batch_and_the_on_hand_together_TS_INV_063()
    {
        var seed = await SeedBatchAsync(quantity: 10m);

        (await WriteOffAsync(seed.BatchId, 3m, "damaged")).StatusCode.ShouldBe(HttpStatusCode.Created);

        (await RemainingAsync(seed.BatchId)).ShouldBe(7m);
        (await OnHandAsync(seed.ProductId)).ShouldBe(7m);
        (await ReceivedAsync(seed.BatchId)).ShouldBe(10m);   // history never changes
    }

    [Fact]
    public async Task Only_the_write_off_reason_list_is_accepted_TS_INV_064()
    {
        var seed = await SeedBatchAsync(quantity: 20m);

        foreach (var allowed in new[] { "expired", "contaminated", "damaged", "lost", "other" })
        {
            (await WriteOffAsync(seed.BatchId, 1m, allowed)).StatusCode.ShouldBe(HttpStatusCode.Created);
        }

        // The adjustment-only reasons are not members of the write-off contract at all
        // (DEC-INV-031) — «موجود» on a write-off would be a contradiction in terms.
        foreach (var adjustmentOnly in new[] { "countCorrection", "initialBalance", "found" })
        {
            (await WriteOffAsync(seed.BatchId, 1m, adjustmentOnly)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        (await RemainingAsync(seed.BatchId)).ShouldBe(15m);   // the five legal ones only
    }

    [Fact]
    public async Task A_write_off_beyond_the_batch_is_rejected_whole_TS_INV_065()
    {
        var seed = await SeedBatchAsync(quantity: 5m);

        var rejected = await WriteOffAsync(seed.BatchId, 6m, "damaged");

        rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(rejected)).ShouldBe(InventoryErrorCodes.QuantityBelowZero);
        // Nothing was clamped to zero and no ledger row was written (BR-INV-061, DEC-INV-032).
        (await RemainingAsync(seed.BatchId)).ShouldBe(5m);
        (await OnHandAsync(seed.ProductId)).ShouldBe(5m);
        (await MovementsAsync(seed.BatchId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Expired_stock_can_finally_leave_inventory_TS_INV_066_R9()
    {
        // The exact situation R9 described: a batch that is expired, therefore unsaleable, and
        // therefore stranded inside the on-hand quantity with no exit.
        var seed = await SeedBatchAsync(quantity: 8m, expiry: fixture.ClinicToday.AddDays(-1));

        (await WriteOffAsync(seed.BatchId, 8m, "expired", actorName: "الطبيب")).StatusCode
            .ShouldBe(HttpStatusCode.Created);

        (await RemainingAsync(seed.BatchId)).ShouldBe(0m);   // depleted by derivation, no new state
        (await OnHandAsync(seed.ProductId)).ShouldBe(0m);

        var movement = (await MovementsAsync(seed.BatchId)).Single();
        movement.Type.ShouldBe(InventoryMovementType.WriteOff);
        movement.Quantity.ShouldBe(-8m);
        movement.Source.ShouldBe(InventoryMovementSource.Inventory);
        movement.ReferenceId.ShouldBeNull();
        movement.Reason.ShouldBe(InventoryMovementReason.Expired);
        movement.ActorName.ShouldBe("الطبيب");
    }

    // ---- seeding and helpers ----------------------------------------------------------------------

    private async Task<(Guid ProductId, Guid BatchId)> SeedBatchAsync(decimal quantity, DateOnly? expiry = null)
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var productId = Guid.Empty;
        var batchId = Guid.Empty;

        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            productId = CatalogSeeder.NewProduct(
                dbContext, $"منتج {marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature,
                hasExpiration: expiry is not null).Id;

            batchId = InventorySeeder.AddBatch(dbContext, productId, quantity, expiry).Id;
            InventorySeeder.SetOnHand(dbContext, productId, quantity);
            return Task.CompletedTask;
        });

        return (productId, batchId);
    }

    private Task<HttpResponseMessage> WriteOffAsync(
        Guid batchId,
        decimal quantity,
        string reason,
        string? note = null,
        string? actorName = null) =>
        fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/inventory/write-offs", UriKind.Relative),
            new { batchId, quantity, reason, reasonNote = note, actorName });

    private Task<decimal> RemainingAsync(Guid batchId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.Id == batchId).Select(batch => batch.RemainingQuantity).SingleAsync());

    private Task<decimal> ReceivedAsync(Guid batchId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.Id == batchId).Select(batch => batch.Quantity).SingleAsync());

    private Task<decimal> OnHandAsync(Guid productId) =>
        fixture.QueryDbAsync(dbContext => dbContext.ProductOnHands
            .Where(item => item.ProductId == productId).Select(item => item.OnHandQuantity).SingleAsync());

    private Task<List<InventoryMovement>> MovementsAsync(Guid batchId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryMovements
            .Where(movement => movement.BatchId == batchId).ToListAsync());

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("errorCode").GetString();
    }
}
