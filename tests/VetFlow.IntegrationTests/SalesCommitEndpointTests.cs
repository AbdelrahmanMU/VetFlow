using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using VetFlow.Domain.Catalog;
using VetFlow.Domain.Inventory;
using VetFlow.Domain.Sales;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Committing a sale (REQ-SAL-003) and its inventory effect — consumption (REQ-INV-006) and FEFO
/// allocation (REQ-INV-007) — end to end through the real API and a real PostgreSQL.
///
/// Covers TS-SAL-008..015 and TS-INV-037..047, TS-INV-049..051, TS-INV-053..055: the state
/// transition and the decrement, unit conversion, one-time commit, full rejection on shortage,
/// immutability, atomicity, expired-batch exclusion with its one-day boundary, per-line
/// traceability, exact quantities, and the read-side integration. Every assertion that matters is
/// verified against the database, not the HTTP response alone.
///
/// The seeded product chain is carton ×12 → box ×10 → strip (stock unit, and the smallest), so a
/// box is 10 strips.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class SalesCommitEndpointTests(ApiFixture fixture)
{
    private const decimal BoxToStrip = 10m;

    [Fact]
    public async Task Committing_transitions_the_invoice_and_decrements_stock_TS_SAL_008()
    {
        // 12 strips sold from a single batch of 50: on-hand 50 → 38 (AC-SAL-007, AC-INV-037).
        var product = await SeedProductWithStockAsync(batches: [(50m, null)], stripPrice: 5m);
        var invoice = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 12m);

        (await CommitAsync(invoice)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await StatusAsync(invoice)).ShouldBe(SalesInvoiceStatus.Committed);
        (await OnHandAsync(product.ProductId)).ShouldBe(38m);

        var batches = await BatchesAsync(product.ProductId);
        batches.Single().RemainingQuantity.ShouldBe(38m);
        batches.Single().Quantity.ShouldBe(50m);   // the received quantity never changes
    }

    [Fact]
    public async Task The_sale_unit_is_converted_into_the_stock_unit_TS_SAL_009()
    {
        // 2 boxes = 20 strips deducted, not 2 (BR-SAL-010).
        var product = await SeedProductWithStockAsync(batches: [(50m, null)], boxPrice: 50m);
        var invoice = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.BoxUnit, 2m);

        (await CommitAsync(invoice)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await OnHandAsync(product.ProductId)).ShouldBe(50m - (2m * BoxToStrip));
    }

    [Fact]
    public async Task Committing_twice_is_rejected_and_never_deducts_twice_TS_SAL_010()
    {
        var product = await SeedProductWithStockAsync(batches: [(50m, null)], stripPrice: 5m);
        var invoice = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 12m);
        (await CommitAsync(invoice)).EnsureSuccessStatusCode();

        var second = await CommitAsync(invoice);

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(second)).ShouldBe(SalesErrorCodes.InvoiceNotDraft);
        (await OnHandAsync(product.ProductId)).ShouldBe(38m);   // deducted once, not twice
    }

    [Fact]
    public async Task Committing_an_empty_invoice_is_rejected_without_effect_TS_SAL_010()
    {
        var invoice = await CreateInvoiceAsync();

        var response = await CommitAsync(invoice);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(response)).ShouldBe(SalesErrorCodes.InvoiceHasNoLines);
        (await StatusAsync(invoice)).ShouldBe(SalesInvoiceStatus.Draft);
    }

    [Fact]
    public async Task Insufficient_stock_on_one_line_rejects_the_whole_invoice_TS_SAL_011()
    {
        var plentiful = await SeedProductWithStockAsync(batches: [(100m, null)], stripPrice: 5m);
        var scarce = await SeedProductWithStockAsync(batches: [(5m, null)], stripPrice: 5m);

        var invoice = await CreateInvoiceAsync();
        await AddLineAsync(invoice, plentiful.ProductId, SeededCatalogIds.StripUnit, 10m);
        await AddLineAsync(invoice, scarce.ProductId, SeededCatalogIds.StripUnit, 10m);

        var response = await CommitAsync(invoice);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(response)).ShouldBe(InventoryErrorCodes.InsufficientStock);
        // The message names the product that fell short, and only that one (AC-SAL-009).
        var named = await DetailAsync(response, "products");
        named.ShouldBe(scarce.Name);

        // Nothing moved — not even the batches of the line that had plenty (BR-INV-052).
        (await StatusAsync(invoice)).ShouldBe(SalesInvoiceStatus.Draft);
        (await OnHandAsync(plentiful.ProductId)).ShouldBe(100m);
        (await OnHandAsync(scarce.ProductId)).ShouldBe(5m);
        (await BatchesAsync(plentiful.ProductId)).Single().RemainingQuantity.ShouldBe(100m);
        (await LinesAsync(invoice)).GetArrayLength().ShouldBe(2);   // no line is lost
    }

    [Fact]
    public async Task A_committed_invoice_is_immutable_TS_SAL_012()
    {
        var product = await SeedProductWithStockAsync(batches: [(50m, null)], stripPrice: 5m);
        var invoice = await CreateInvoiceAsync();
        var lineId = await AddLineAsync(invoice, product.ProductId, SeededCatalogIds.StripUnit, 5m);
        (await CommitAsync(invoice)).EnsureSuccessStatusCode();

        (await AddLineResponseAsync(invoice, product.ProductId, SeededCatalogIds.StripUnit, 1m))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await fixture.Client.DeleteAsync(LineUri(invoice, lineId)))
            .StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await LinesAsync(invoice)).GetArrayLength().ShouldBe(1);
        (await TotalAsync(invoice)).ShouldBe(25m);
    }

    [Fact]
    public async Task A_failure_midway_through_the_write_commits_nothing_TS_SAL_013()
    {
        // A product with saleable batches but no on-hand row is a broken invariant: the writer has
        // already staged the batch decrements in memory when it throws. Because everything hangs on
        // one SaveChanges, the database must be untouched (BR-INV-048, AC-INV-042, TS-INV-049).
        var product = await SeedProductWithStockAsync(batches: [(50m, null)], stripPrice: 5m);
        await fixture.SeedAsync(async dbContext =>
        {
            var onHand = await dbContext.ProductOnHands.FirstAsync(item => item.ProductId == product.ProductId);
            dbContext.ProductOnHands.Remove(onHand);
        });

        var invoice = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 12m);

        var response = await CommitAsync(invoice);
        response.IsSuccessStatusCode.ShouldBeFalse();

        (await StatusAsync(invoice)).ShouldBe(SalesInvoiceStatus.Draft);
        (await BatchesAsync(product.ProductId)).Single().RemainingQuantity.ShouldBe(50m);
        (await ConsumptionsAsync(product.ProductId)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Fefo_consumes_the_nearest_expiry_first_TS_INV_038()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var product = await SeedProductWithStockAsync(
            batches: [(100m, today.AddDays(60)), (100m, today.AddDays(10))], stripPrice: 5m);
        var invoice = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 30m);

        (await CommitAsync(invoice)).EnsureSuccessStatusCode();

        var batches = await BatchesByExpiryAsync(product.ProductId);
        batches[0].RemainingQuantity.ShouldBe(70m);   // expires in 10 days — consumed
        batches[1].RemainingQuantity.ShouldBe(100m);  // expires in 60 days — untouched
    }

    [Fact]
    public async Task Fefo_spans_batches_and_depletes_the_nearest_first_TS_INV_039()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var product = await SeedProductWithStockAsync(
            batches: [(20m, today.AddDays(10)), (100m, today.AddDays(60))], stripPrice: 5m);
        var invoice = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 50m);

        (await CommitAsync(invoice)).EnsureSuccessStatusCode();

        var batches = await BatchesByExpiryAsync(product.ProductId);
        batches[0].RemainingQuantity.ShouldBe(0m);    // drained, and kept — never deleted
        batches[1].RemainingQuantity.ShouldBe(70m);
        (await OnHandAsync(product.ProductId)).ShouldBe(70m);
    }

    [Fact]
    public async Task Batches_without_an_expiry_are_saleable_and_allocated_last_TS_INV_041()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var product = await SeedProductWithStockAsync(
            batches: [(50m, null), (50m, today.AddDays(90))], stripPrice: 5m);

        var first = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 30m);
        (await CommitAsync(first)).EnsureSuccessStatusCode();

        var dated = await fixture.QueryDbAsync(db => db.InventoryBatches
            .Where(batch => batch.ProductId == product.ProductId && batch.ExpiryDate != null)
            .SingleAsync());
        var undated = await fixture.QueryDbAsync(db => db.InventoryBatches
            .Where(batch => batch.ProductId == product.ProductId && batch.ExpiryDate == null)
            .SingleAsync());
        dated.RemainingQuantity.ShouldBe(20m);
        undated.RemainingQuantity.ShouldBe(50m);   // never expires, so no urgency — taken last

        // 30 more drains the dated batch and spills into the undated one.
        var second = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 30m);
        (await CommitAsync(second)).EnsureSuccessStatusCode();

        (await ReloadAsync(dated)).RemainingQuantity.ShouldBe(0m);
        (await ReloadAsync(undated)).RemainingQuantity.ShouldBe(40m);
    }

    [Fact]
    public async Task Expired_batches_are_excluded_before_allocation_and_the_boundary_is_one_day_TS_INV_050()
    {
        var today = ClinicToday();
        var product = await SeedProductWithStockAsync(
            batches: [(100m, today.AddDays(-1)), (100m, today.AddDays(60))], stripPrice: 5m);
        var invoice = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 30m);

        (await CommitAsync(invoice)).EnsureSuccessStatusCode();

        var expired = await BatchWithExpiryAsync(product.ProductId, today.AddDays(-1));
        var valid = await BatchWithExpiryAsync(product.ProductId, today.AddDays(60));
        // The expired batch sorts first under FEFO by construction, yet nothing is taken from it.
        expired.RemainingQuantity.ShouldBe(100m);
        valid.RemainingQuantity.ShouldBe(70m);
    }

    [Fact]
    public async Task A_batch_expiring_today_is_still_saleable_TS_INV_050()
    {
        // ExpiryDate is the last saleable day (BR-INV-059) — today is still inside it.
        var today = ClinicToday();
        var product = await SeedProductWithStockAsync(batches: [(40m, today)], stripPrice: 5m);
        var invoice = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 10m);

        (await CommitAsync(invoice)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await BatchesAsync(product.ProductId)).Single().RemainingQuantity.ShouldBe(30m);
    }

    [Fact]
    public async Task A_product_whose_batches_are_all_expired_reads_as_insufficient_TS_INV_051()
    {
        var today = ClinicToday();
        var product = await SeedProductWithStockAsync(batches: [(100m, today.AddDays(-1))], stripPrice: 5m);
        var invoice = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 10m);

        var response = await CommitAsync(invoice);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(response)).ShouldBe(InventoryErrorCodes.InsufficientStock);
        (await StatusAsync(invoice)).ShouldBe(SalesInvoiceStatus.Draft);

        // The projection still shows the physical balance, and the batch is still "active" in the
        // batch viewer — the exclusion applies to allocation, not to display (BR-INV-054).
        (await OnHandAsync(product.ProductId)).ShouldBe(100m);
        (await BatchesAsync(product.ProductId)).Single().RemainingQuantity.ShouldBe(100m);

        // Adding a valid batch makes the same sale succeed, deducting from it alone.
        await fixture.SeedAsync(dbContext =>
        {
            InventorySeeder.AddBatch(dbContext, product.ProductId, 30m, today.AddDays(30));
            return Task.CompletedTask;
        });
        await fixture.SeedAsync(async dbContext =>
        {
            var onHand = await dbContext.ProductOnHands.FirstAsync(item => item.ProductId == product.ProductId);
            onHand.Increase(30m);
        });

        (await CommitAsync(invoice)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await BatchWithExpiryAsync(product.ProductId, today.AddDays(-1))).RemainingQuantity.ShouldBe(100m);
        (await BatchWithExpiryAsync(product.ProductId, today.AddDays(30))).RemainingQuantity.ShouldBe(20m);
    }

    [Fact]
    public async Task Sufficiency_is_exact_at_the_boundary_TS_INV_043()
    {
        var product = await SeedProductWithStockAsync(batches: [(20m, null), (30m, null)], stripPrice: 5m);

        // One unit more than the total is refused.
        var tooMuch = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 51m);
        (await CommitAsync(tooMuch)).StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await OnHandAsync(product.ProductId)).ShouldBe(50m);

        // Exactly the total succeeds and leaves everything at zero.
        var exact = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 50m);
        (await CommitAsync(exact)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await OnHandAsync(product.ProductId)).ShouldBe(0m);
        (await BatchesAsync(product.ProductId)).ShouldAllBe(batch => batch.RemainingQuantity == 0m);
    }

    [Fact]
    public async Task The_same_product_on_two_lines_is_aggregated_once_TS_INV_047()
    {
        var product = await SeedProductWithStockAsync(batches: [(50m, null)], stripPrice: 5m);
        var invoice = await CreateInvoiceAsync();
        await AddLineAsync(invoice, product.ProductId, SeededCatalogIds.StripUnit, 3m);
        await AddLineAsync(invoice, product.ProductId, SeededCatalogIds.StripUnit, 4m);

        (await CommitAsync(invoice)).EnsureSuccessStatusCode();

        (await OnHandAsync(product.ProductId)).ShouldBe(43m);   // exactly 7, no more and no less
    }

    [Fact]
    public async Task Traceability_reaches_from_the_sale_line_to_every_batch_it_consumed_TS_INV_053()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var product = await SeedProductWithStockAsync(
            batches: [(20m, today.AddDays(10)), (100m, today.AddDays(60))], stripPrice: 5m);
        var invoice = await CreateInvoiceAsync();
        var lineId = await AddLineAsync(invoice, product.ProductId, SeededCatalogIds.StripUnit, 50m);

        (await CommitAsync(invoice)).EnsureSuccessStatusCode();

        var trace = (await fixture.QueryDbAsync(db => db.InventoryMovements
                .Where(movement => movement.ReferenceId == lineId
                    && movement.Type == InventoryMovementType.Consume)
                .ToListAsync()))
            .Select(ToTrace)
            .ToList();

        trace.Count.ShouldBe(2);                       // 20 from the nearest, 30 from the next
        trace.Sum(entry => entry.Quantity).ShouldBe(50m);
        trace.Select(entry => entry.BatchId).Distinct().Count().ShouldBe(2);
        trace.ShouldAllBe(entry => entry.ProductId == product.ProductId);

        // The relationship survives the batch becoming depleted.
        var depleted = (await BatchesAsync(product.ProductId)).First(batch => batch.RemainingQuantity == 0m);
        trace.ShouldContain(entry => entry.BatchId == depleted.Id && entry.Quantity == 20m);
    }

    [Fact]
    public async Task Traceability_keeps_two_lines_of_the_same_product_apart_TS_INV_054()
    {
        var product = await SeedProductWithStockAsync(batches: [(50m, null)], stripPrice: 5m);
        var invoice = await CreateInvoiceAsync();
        var firstLine = await AddLineAsync(invoice, product.ProductId, SeededCatalogIds.StripUnit, 3m);
        var secondLine = await AddLineAsync(invoice, product.ProductId, SeededCatalogIds.StripUnit, 4m);

        (await CommitAsync(invoice)).EnsureSuccessStatusCode();

        var trace = await ConsumptionsAsync(product.ProductId);
        trace.Where(entry => entry.SaleLineId == firstLine).Sum(entry => entry.Quantity).ShouldBe(3m);
        trace.Where(entry => entry.SaleLineId == secondLine).Sum(entry => entry.Quantity).ShouldBe(4m);
    }

    [Fact]
    public async Task Two_sales_from_the_same_batch_each_trace_their_own_share_TS_INV_053()
    {
        var product = await SeedProductWithStockAsync(batches: [(50m, null)], stripPrice: 5m);

        var first = await CreateInvoiceAsync();
        var firstLine = await AddLineAsync(first, product.ProductId, SeededCatalogIds.StripUnit, 10m);
        (await CommitAsync(first)).EnsureSuccessStatusCode();

        var second = await CreateInvoiceAsync();
        var secondLine = await AddLineAsync(second, product.ProductId, SeededCatalogIds.StripUnit, 15m);
        (await CommitAsync(second)).EnsureSuccessStatusCode();

        var trace = await ConsumptionsAsync(product.ProductId);
        trace.Single(entry => entry.SaleLineId == firstLine).Quantity.ShouldBe(10m);
        trace.Single(entry => entry.SaleLineId == secondLine).Quantity.ShouldBe(15m);
    }

    [Fact]
    public async Task The_consistency_invariant_holds_after_consumption_TS_INV_044()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var first = await SeedProductWithStockAsync(
            batches: [(20m, today.AddDays(10)), (100m, today.AddDays(60))], stripPrice: 5m);
        var second = await SeedProductWithStockAsync(batches: [(80m, null)], stripPrice: 5m);

        var invoice = await CreateInvoiceAsync();
        await AddLineAsync(invoice, first.ProductId, SeededCatalogIds.StripUnit, 35m);
        await AddLineAsync(invoice, second.ProductId, SeededCatalogIds.StripUnit, 12m);
        (await CommitAsync(invoice)).EnsureSuccessStatusCode();

        foreach (var productId in new[] { first.ProductId, second.ProductId })
        {
            var onHand = await OnHandAsync(productId);
            var batchSum = (await BatchesAsync(productId))
                .Where(batch => batch.RemainingQuantity > 0m)
                .Sum(batch => batch.RemainingQuantity);
            onHand.ShouldBe(batchSum);   // BR-INV-005, verified straight from the database
        }
    }

    [Fact]
    public async Task Quantities_stay_exact_and_leave_no_ghost_balance_TS_INV_055()
    {
        // 3 boxes × 10 strips = exactly 30 strips; the balance lands on zero, not 0.0000001.
        var product = await SeedProductWithStockAsync(batches: [(30m, null)], boxPrice: 50m);
        var invoice = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.BoxUnit, 3m);

        (await CommitAsync(invoice)).EnsureSuccessStatusCode();

        (await OnHandAsync(product.ProductId)).ShouldBe(0m);
        (await BatchesAsync(product.ProductId)).Single().RemainingQuantity.ShouldBe(0m);
    }

    [Fact]
    public async Task Selling_the_smallest_unit_alone_deducts_one_TS_INV_055()
    {
        var product = await SeedProductWithStockAsync(batches: [(30m, null)], stripPrice: 5m);
        var invoice = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 1m);

        (await CommitAsync(invoice)).EnsureSuccessStatusCode();

        (await OnHandAsync(product.ProductId)).ShouldBe(29m);
    }

    [Fact]
    public async Task An_inexact_conversion_is_rejected_and_never_rounded_TS_SAL_015()
    {
        // A product configured AGAINST the amended BR-CAT-020 (DEC-CAT-033): its stock unit is the
        // middle unit, box = 3 strips, so selling 1 strip would need 1/3 of a box. The rule says
        // reject — never round, never truncate (BR-INV-058, AC-SAL-013). Such configurations are
        // exactly what DEC-CAT-033 requires to be found and corrected.
        var product = await SeedMisconfiguredProductAsync();
        var invoice = await DraftWithLineAsync(product, SeededCatalogIds.StripUnit, 1m);

        var response = await CommitAsync(invoice);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ErrorCodeAsync(response)).ShouldBe(SalesErrorCodes.InexactUnitConversion);
        (await DetailAsync(response, "reason")).ShouldBe("conversionNotExact");
        (await StatusAsync(invoice)).ShouldBe(SalesInvoiceStatus.Draft);
        (await OnHandAsync(product)).ShouldBe(30m);
        (await BatchesAsync(product)).Single().RemainingQuantity.ShouldBe(30m);
    }

    [Fact]
    public async Task An_exact_conversion_on_the_same_profile_succeeds_TS_SAL_015()
    {
        // The same misconfigured product sold in whole boxes converts exactly (3 strips ÷ 3 = 1),
        // so the rejection above is about exactness, not about the product.
        var product = await SeedMisconfiguredProductAsync();
        var invoice = await DraftWithLineAsync(product, SeededCatalogIds.BoxUnit, 2m);

        (await CommitAsync(invoice)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await OnHandAsync(product)).ShouldBe(28m);   // 2 boxes deducted, a whole number
    }

    [Fact]
    public async Task The_read_screens_reflect_the_consumption_without_changing_TS_INV_045()
    {
        // The batch viewer resolves the purchase reference through the originating purchase line
        // (BR-INV-024), so this one needs a batch with real provenance, the way receiving makes it.
        var today = ClinicToday();
        var product = await SeedProductWithProvenanceAsync(quantity: 20m, expiry: today.AddDays(10));

        // Before: the batch is active and visible in expiry monitoring.
        (await ExpiryMonitoringContainsAsync(product.ProductId)).ShouldBeTrue();

        var invoice = await DraftWithLineAsync(product.ProductId, SeededCatalogIds.StripUnit, 20m);
        (await CommitAsync(invoice)).EnsureSuccessStatusCode();

        // The batch viewer still shows the batch, now depleted (BR-INV-021) — never deleted.
        var batchRows = await BatchViewerRowsAsync(product.ProductId);
        batchRows.EnumerateArray().Count().ShouldBe(1);
        batchRows.EnumerateArray().Single().GetProperty("status").GetString().ShouldBe("depleted");

        // The projection shows zero and the product appears under "out of stock".
        (await ProjectionOnHandAsync(product.ProductId)).ShouldBe(0m);
        (await ProjectionContainsOutOfStockAsync(product.ProductId)).ShouldBeTrue();

        // Expiry monitoring drops the depleted batch (scope: RemainingQuantity > 0 — TS-INV-046).
        (await ExpiryMonitoringContainsAsync(product.ProductId)).ShouldBeFalse();
    }

    [Fact]
    public async Task Committing_a_missing_invoice_answers_not_found_REQ_SAL_003()
    {
        (await CommitAsync(Guid.NewGuid())).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ---- seeding ------------------------------------------------------------------------------

    private async Task<(Guid ProductId, string Name)> SeedProductWithStockAsync(
        (decimal Quantity, DateOnly? Expiry)[] batches,
        decimal? boxPrice = null,
        decimal? stripPrice = null)
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var productId = Guid.Empty;
        var name = $"منتج {marker}";

        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            var product = CatalogSeeder.NewProduct(
                dbContext, name, category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature,
                isSplittable: true, hasExpiration: batches.Any(batch => batch.Expiry is not null),
                boxPrice: boxPrice, stripPrice: stripPrice);
            productId = product.Id;

            foreach (var batch in batches)
            {
                InventorySeeder.AddBatch(dbContext, productId, batch.Quantity, batch.Expiry);
            }

            InventorySeeder.SetOnHand(dbContext, productId, batches.Sum(batch => batch.Quantity));
            return Task.CompletedTask;
        });

        return (productId, name);
    }

    /// <summary>
    /// A product whose single batch carries real purchase provenance — needed by any assertion that
    /// goes through the batch viewer, which resolves the purchase reference through that chain.
    /// </summary>
    private async Task<(Guid ProductId, string Name)> SeedProductWithProvenanceAsync(decimal quantity, DateOnly? expiry)
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var productId = Guid.Empty;
        var name = $"منتج {marker}";

        await fixture.SeedAsync(async dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            var product = CatalogSeeder.NewProduct(
                dbContext, name, category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature,
                isSplittable: true, hasExpiration: expiry is not null, stripPrice: 5m);
            productId = product.Id;

            await InventorySeeder.AddBatchWithProvenanceAsync(dbContext, productId, name, quantity, expiry);
            InventorySeeder.SetOnHand(dbContext, productId, quantity);
        });

        return (productId, name);
    }

    /// <summary>
    /// A product whose stock unit is <b>not</b> the smallest — carton ×12 → box ×3 → strip, with the
    /// box as the stock unit. This violates BR-CAT-020 as amended by DEC-CAT-033 and is seeded
    /// deliberately: it is the only way a conversion can be inexact, and therefore the only way to
    /// exercise the rejection BR-INV-058 requires.
    /// </summary>
    private async Task<Guid> SeedMisconfiguredProductAsync()
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var productId = Guid.Empty;

        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            var units = new List<ProductUnit>
            {
                new(Guid.NewGuid(), SeededCatalogIds.CartonUnit, 0, 12, isPurchaseUnit: true, isSaleUnit: false),
                new(Guid.NewGuid(), SeededCatalogIds.BoxUnit, 1, 3, isPurchaseUnit: false, isSaleUnit: true, sellingPrice: 60m),
                new(Guid.NewGuid(), SeededCatalogIds.StripUnit, 2, null, isPurchaseUnit: false, isSaleUnit: true, sellingPrice: 25m),
            };
            var product = new Product(
                Guid.NewGuid(), $"PRD-SEED-{Guid.NewGuid():N}", $"منتج غير مطابق {marker}",
                category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature,
                new ProductCapabilities(true, false, false, false, null), units,
                storageUnitId: SeededCatalogIds.BoxUnit,
                defaultSaleUnitId: SeededCatalogIds.BoxUnit,
                defaultPurchaseUnitId: SeededCatalogIds.CartonUnit);
            dbContext.Products.Add(product);
            productId = product.Id;

            InventorySeeder.AddBatch(dbContext, productId, 30m);
            InventorySeeder.SetOnHand(dbContext, productId, 30m);
            return Task.CompletedTask;
        });

        return productId;
    }

    // ---- API helpers --------------------------------------------------------------------------

    private async Task<Guid> CreateInvoiceAsync()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            InvoicesUri, new { saleDate = new DateOnly(2026, 7, 30) });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<Guid> DraftWithLineAsync(Guid productId, Guid saleUnitId, decimal quantity)
    {
        var invoiceId = await CreateInvoiceAsync();
        await AddLineAsync(invoiceId, productId, saleUnitId, quantity);
        return invoiceId;
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

    private Task<HttpResponseMessage> CommitAsync(Guid invoiceId) =>
        fixture.Client.PostAsync(new Uri($"/api/v1/sales-invoices/{invoiceId}/commit", UriKind.Relative), content: null);

    private async Task<JsonElement> LinesAsync(Guid invoiceId)
    {
        var response = await fixture.Client.GetAsync(LinesUri(invoiceId));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.Clone();
    }

    private async Task<decimal> TotalAsync(Guid invoiceId)
    {
        var response = await fixture.Client.GetAsync(new Uri($"/api/v1/sales-invoices/{invoiceId}", UriKind.Relative));
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("total").GetProperty("amount").GetDecimal();
    }

    private async Task<JsonElement> BatchViewerRowsAsync(Guid productId)
    {
        var response = await fixture.Client.GetAsync(new Uri($"/api/v1/inventory/{productId}/batches", UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("batches").GetProperty("items").Clone();
    }

    private async Task<decimal> ProjectionOnHandAsync(Guid productId)
    {
        using var body = await GetJsonAsync("/api/v1/inventory?pageSize=100");
        return body.RootElement.GetProperty("items").EnumerateArray()
            .Single(item => item.GetProperty("productId").GetGuid() == productId)
            .GetProperty("onHandQuantity").GetDecimal();
    }

    private async Task<bool> ProjectionContainsOutOfStockAsync(Guid productId)
    {
        using var body = await GetJsonAsync("/api/v1/inventory?outOfStock=true&pageSize=100");
        return body.RootElement.GetProperty("items").EnumerateArray()
            .Any(item => item.GetProperty("productId").GetGuid() == productId);
    }

    private async Task<bool> ExpiryMonitoringContainsAsync(Guid productId)
    {
        using var body = await GetJsonAsync("/api/v1/inventory/expiry?pageSize=100");
        return body.RootElement.GetProperty("items").EnumerateArray()
            .Any(item => item.GetProperty("productId").GetGuid() == productId);
    }

    private async Task<JsonDocument> GetJsonAsync(string relativeUri)
    {
        var response = await fixture.Client.GetAsync(new Uri(relativeUri, UriKind.Relative));
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    }

    // ---- database helpers ---------------------------------------------------------------------

    private Task<SalesInvoiceStatus> StatusAsync(Guid invoiceId) =>
        fixture.QueryDbAsync(dbContext => dbContext.SalesInvoices
            .Where(invoice => invoice.Id == invoiceId)
            .Select(invoice => invoice.Status)
            .FirstAsync());

    private Task<decimal> OnHandAsync(Guid productId) =>
        fixture.QueryDbAsync(dbContext => dbContext.ProductOnHands
            .Where(item => item.ProductId == productId)
            .Select(item => item.OnHandQuantity)
            .FirstAsync());

    private Task<List<InventoryBatch>> BatchesAsync(Guid productId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.ProductId == productId)
            .ToListAsync());

    private Task<List<InventoryBatch>> BatchesByExpiryAsync(Guid productId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.ProductId == productId)
            .OrderBy(batch => batch.ExpiryDate)
            .ToListAsync());

    private Task<InventoryBatch> BatchWithExpiryAsync(Guid productId, DateOnly expiry) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.ProductId == productId && batch.ExpiryDate == expiry)
            .SingleAsync());

    private Task<InventoryBatch> ReloadAsync(InventoryBatch batch) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches.SingleAsync(item => item.Id == batch.Id));

    /// <summary>
    /// Sale-line-level consumption traceability (REQ-INV-008, BR-INV-057), read from the unified
    /// movement ledger that absorbed the Sprint 7 InventoryConsumption record (DEC-INV-027). The
    /// requirement is unchanged — only where it is stored. Ledger quantities are signed
    /// (BR-INV-064), so the magnitude is projected here and the assertions stay in business terms.
    /// </summary>
    private sealed record Trace(Guid BatchId, Guid ProductId, Guid SaleLineId, decimal Quantity);

    private async Task<List<Trace>> ConsumptionsAsync(Guid productId) =>
        (await fixture.QueryDbAsync(dbContext => dbContext.InventoryMovements
            .Where(movement => movement.ProductId == productId
                && movement.Type == InventoryMovementType.Consume)
            .ToListAsync()))
        .Select(ToTrace)
        .ToList();

    private static Trace ToTrace(InventoryMovement movement) =>
        new(movement.BatchId, movement.ProductId, movement.ReferenceId!.Value, -movement.Quantity);

    /// <summary>
    /// Today at the clinic — the same basis the server uses (BR-INV-059/060). Resolved through the
    /// API's own clock rather than a second copy of the time-zone id, so the test cannot drift from
    /// the configured zone.
    /// </summary>
    private DateOnly ClinicToday() => fixture.ClinicToday;

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("errorCode").GetString();
    }

    /// <summary>Reads one structured fact from the problem document's metadata extension.</summary>
    private static async Task<string?> DetailAsync(HttpResponseMessage response, string key)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.TryGetProperty("metadata", out var metadata)
            && metadata.TryGetProperty(key, out var value)
                ? value.GetString()
                : null;
    }

    private static readonly Uri InvoicesUri = new("/api/v1/sales-invoices", UriKind.Relative);

    private static Uri LinesUri(Guid invoiceId) => new($"/api/v1/sales-invoices/{invoiceId}/lines", UriKind.Relative);

    private static Uri LineUri(Guid invoiceId, Guid lineId) =>
        new($"/api/v1/sales-invoices/{invoiceId}/lines/{lineId}", UriKind.Relative);
}
