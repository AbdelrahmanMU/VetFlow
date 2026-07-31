using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VetFlow.Domain.Inventory;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Inventory adjustments (REQ-INV-010) — POST /api/v1/inventory/adjustments, end to end through
/// the real API and a real PostgreSQL.
///
/// Covers TS-INV-057..062: both directions moving the batch and the on-hand quantity together
/// (AC-INV-051, the BR-INV-005 invariant that closes R5), the never-negative rejection with no
/// partial effect and no clamping (AC-INV-052), the adjustment-only reason list (AC-INV-053), the
/// optional actor (AC-INV-054), and the single ledger row that shows up in the history with no
/// reference (AC-INV-055).
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class InventoryAdjustmentEndpointTests(ApiFixture fixture)
{
    [Fact]
    public async Task A_positive_adjustment_raises_the_batch_and_the_on_hand_together_TS_INV_057()
    {
        var seed = await SeedBatchAsync(quantity: 10m);

        var response = await AdjustAsync(seed.BatchId, "increase", 4m, "found");

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await RemainingAsync(seed.BatchId)).ShouldBe(14m);
        (await OnHandAsync(seed.ProductId)).ShouldBe(14m);
        // The historical received quantity is untouched.
        (await ReceivedAsync(seed.BatchId)).ShouldBe(10m);
    }

    [Fact]
    public async Task A_negative_adjustment_lowers_both_and_an_excessive_one_is_rejected_whole_TS_INV_058()
    {
        var seed = await SeedBatchAsync(quantity: 10m);

        (await AdjustAsync(seed.BatchId, "decrease", 4m, "lost")).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await RemainingAsync(seed.BatchId)).ShouldBe(6m);
        (await OnHandAsync(seed.ProductId)).ShouldBe(6m);

        var rejected = await AdjustAsync(seed.BatchId, "decrease", 99m, "lost");

        rejected.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(rejected)).ShouldBe(InventoryErrorCodes.QuantityBelowZero);
        // Nothing moved and nothing was clamped to zero (DEC-INV-032) — and no row was written.
        (await RemainingAsync(seed.BatchId)).ShouldBe(6m);
        (await OnHandAsync(seed.ProductId)).ShouldBe(6m);
        (await MovementsAsync(seed.BatchId)).Count(movement => movement.Type == InventoryMovementType.Adjustment)
            .ShouldBe(1);
    }

    [Fact]
    public async Task Only_the_adjustment_reason_list_is_accepted_TS_INV_059()
    {
        var seed = await SeedBatchAsync(quantity: 10m);

        (await AdjustAsync(seed.BatchId, "increase", 1m, "countCorrection"))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        // "expired" and "contaminated" belong to write-off alone (DEC-INV-031). They are not even
        // members of the adjustment contract enum, so they fail at the boundary.
        foreach (var writeOffOnly in new[] { "expired", "contaminated" })
        {
            var rejected = await AdjustAsync(seed.BatchId, "increase", 1m, writeOffOnly);
            rejected.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        // Only the one legal adjustment above was applied.
        (await RemainingAsync(seed.BatchId)).ShouldBe(11m);
    }

    [Fact]
    public async Task The_actor_name_is_optional_and_stored_as_written_TS_INV_060()
    {
        var seed = await SeedBatchAsync(quantity: 10m);

        (await AdjustAsync(seed.BatchId, "increase", 1m, "found", actorName: "الكاشير"))
            .StatusCode.ShouldBe(HttpStatusCode.Created);
        (await AdjustAsync(seed.BatchId, "increase", 1m, "found"))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        var movements = (await MovementsAsync(seed.BatchId))
            .Where(movement => movement.Type == InventoryMovementType.Adjustment)
            .ToList();

        movements.Count.ShouldBe(2);
        movements.ShouldContain(movement => movement.ActorName == "الكاشير");
        // Its absence never blocks the operation (BR-INV-066) — there is no users module.
        movements.ShouldContain(movement => movement.ActorName == null);
    }

    [Fact]
    public async Task One_ledger_row_is_written_and_it_appears_in_the_history_with_no_reference_TS_INV_061()
    {
        var seed = await SeedBatchAsync(quantity: 10m);

        await AdjustAsync(seed.BatchId, "increase", 4m, "countCorrection", note: "جرد نصف السنة");

        var movement = (await MovementsAsync(seed.BatchId))
            .Single(candidate => candidate.Type == InventoryMovementType.Adjustment);
        movement.Quantity.ShouldBe(4m);                                   // signed: an increase
        movement.Source.ShouldBe(InventoryMovementSource.Inventory);
        movement.ReferenceId.ShouldBeNull();                              // no counterparty document
        movement.Reason.ShouldBe(InventoryMovementReason.CountCorrection);
        movement.ReasonNote.ShouldBe("جرد نصف السنة");

        // And a reversing adjustment adds a second row rather than editing the first (DEC-INV-037).
        await AdjustAsync(seed.BatchId, "decrease", 4m, "countCorrection");
        var rows = (await MovementsAsync(seed.BatchId))
            .Where(candidate => candidate.Type == InventoryMovementType.Adjustment)
            .Select(candidate => candidate.Quantity)
            .OrderBy(quantity => quantity)
            .ToList();
        rows.ShouldBe([-4m, 4m]);

        // The history screen shows it with a dash, not a broken link (BR-INV-043).
        var historyRow = await HistoryRowAsync(movement.Id);
        historyRow.GetProperty("type").GetString().ShouldBe("adjustment");
        historyRow.GetProperty("source").GetString().ShouldBe("inventory");
        historyRow.GetProperty("referenceTarget").GetString().ShouldBe("none");
        (historyRow.GetProperty("referenceLabel").ValueKind == JsonValueKind.Null).ShouldBeTrue();
    }

    [Fact]
    public async Task An_unknown_batch_is_a_404_and_a_malformed_request_is_a_400()
    {
        (await AdjustAsync(Guid.NewGuid(), "increase", 1m, "found")).StatusCode
            .ShouldBe(HttpStatusCode.NotFound);

        var seed = await SeedBatchAsync(quantity: 10m);
        (await AdjustAsync(seed.BatchId, "increase", 0m, "found")).StatusCode
            .ShouldBe(HttpStatusCode.BadRequest);
        (await AdjustAsync(seed.BatchId, "increase", -5m, "found")).StatusCode
            .ShouldBe(HttpStatusCode.BadRequest);
    }

    // ---- seeding and helpers ----------------------------------------------------------------------

    private async Task<(Guid ProductId, Guid BatchId)> SeedBatchAsync(decimal quantity)
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var productId = Guid.Empty;
        var batchId = Guid.Empty;

        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            productId = CatalogSeeder.NewProduct(
                dbContext, $"منتج {marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature).Id;

            batchId = InventorySeeder.AddBatch(dbContext, productId, quantity).Id;
            InventorySeeder.SetOnHand(dbContext, productId, quantity);
            return Task.CompletedTask;
        });

        return (productId, batchId);
    }

    private Task<HttpResponseMessage> AdjustAsync(
        Guid batchId,
        string direction,
        decimal quantity,
        string reason,
        string? note = null,
        string? actorName = null) =>
        fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/inventory/adjustments", UriKind.Relative),
            new { batchId, direction, quantity, reason, reasonNote = note, actorName });

    private Task<decimal> RemainingAsync(Guid batchId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.Id == batchId)
            .Select(batch => batch.RemainingQuantity)
            .SingleAsync());

    private Task<decimal> ReceivedAsync(Guid batchId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.Id == batchId)
            .Select(batch => batch.Quantity)
            .SingleAsync());

    private Task<decimal> OnHandAsync(Guid productId) =>
        fixture.QueryDbAsync(dbContext => dbContext.ProductOnHands
            .Where(item => item.ProductId == productId)
            .Select(item => item.OnHandQuantity)
            .SingleAsync());

    private Task<List<InventoryMovement>> MovementsAsync(Guid batchId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryMovements
            .Where(movement => movement.BatchId == batchId)
            .ToListAsync());

    private async Task<JsonElement> HistoryRowAsync(Guid movementId)
    {
        var response = await fixture.Client.GetAsync(
            new Uri("/api/v1/inventory/movements?page=1&pageSize=100", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("items").EnumerateArray()
            .Single(row => row.GetProperty("movementId").GetGuid() == movementId)
            .Clone();
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("errorCode").GetString();
    }
}
