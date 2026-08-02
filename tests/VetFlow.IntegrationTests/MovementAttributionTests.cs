using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VetFlow.Domain.Inventory;
using VetFlow.Infrastructure.Persistence.Attribution;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Every operation belongs to an authenticated performer (REQ-IDN-008, AC-IDN-011, and BR-INV-066
/// as amended on 2026-08-02). Attribution used to be an <b>optional free-text</b> name a caller
/// could leave blank or fill in with anything; it is now derived from the token's claims and
/// cannot be supplied at all.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class MovementAttributionTests(ApiFixture fixture)
{
    [Fact]
    public async Task A_movement_records_the_signed_in_user_TS_IDN_013_AC_IDN_011()
    {
        var (productId, batchId) = await SeedBatchAsync(10m);

        var response = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/inventory/adjustments", UriKind.Relative),
            new { batchId, direction = "increase", quantity = 2m, reason = "found" });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var performedBy = await fixture.QueryDbAsync(dbContext => dbContext.InventoryMovements
            .Where(movement => movement.ProductId == productId)
            .Select(movement => EF.Property<Guid>(movement, PerformedBy.UserIdProperty))
            .SingleAsync());

        // The seeded owner — the user the fixture actually signed in as, not a value any request
        // carried (BR-IDN-004).
        performedBy.ShouldNotBe(Guid.Empty);
        performedBy.ShouldBe(await SignedInUserIdAsync());
    }

    [Fact]
    public async Task The_client_cannot_choose_who_performed_it_AC_IDN_011()
    {
        var (productId, batchId) = await SeedBatchAsync(10m);

        // `actorName` is still accepted — the historical field was not removed (BR-INV-066) — and
        // it changes attribution not at all.
        var response = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/inventory/adjustments", UriKind.Relative),
            new { batchId, direction = "increase", quantity = 1m, reason = "found", actorName = "شخص آخر تمامًا" });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var movement = await fixture.QueryDbAsync(dbContext => dbContext.InventoryMovements
            .Where(entry => entry.ProductId == productId)
            .Select(entry => new
            {
                entry.ActorName,
                PerformedBy = EF.Property<Guid>(entry, PerformedBy.UserIdProperty),
            })
            .SingleAsync());

        movement.ActorName.ShouldBe("شخص آخر تمامًا");
        movement.PerformedBy.ShouldBe(await SignedInUserIdAsync());
    }

    private Task<Guid> SignedInUserIdAsync() =>
        fixture.QueryDbAsync(dbContext => dbContext.Users
            .Where(user => user.PhoneNumber == VetFlow.Infrastructure.Organization.OrganizationSeeder.PilotOwnerPhoneNumber)
            .Select(user => user.Id)
            .SingleAsync());

    private async Task<(Guid ProductId, Guid BatchId)> SeedBatchAsync(decimal quantity)
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var productId = Guid.Empty;
        var batchId = Guid.Empty;

        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف إسناد {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع إسناد {marker}");
            productId = CatalogSeeder.NewProduct(
                dbContext, $"منتج إسناد {marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature).Id;

            batchId = InventorySeeder.AddBatch(dbContext, productId, quantity).Id;
            InventorySeeder.SetOnHand(dbContext, productId, quantity);
            return Task.CompletedTask;
        });

        return (productId, batchId);
    }
}
