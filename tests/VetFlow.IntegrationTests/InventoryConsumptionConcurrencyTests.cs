using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shouldly;
using VetFlow.Application.Common;
using VetFlow.Application.Inventory;
using VetFlow.Domain.Inventory;
using VetFlow.Domain.Sales;
using VetFlow.Infrastructure.Inventory;
using VetFlow.Infrastructure.Persistence;

namespace VetFlow.IntegrationTests;

/// <summary>
/// The two properties of consumption that need their own connections to prove: <b>per-batch</b>
/// concurrency detection (BR-INV-056, DEC-INV-023, R6 — TS-INV-052) and a <b>constant</b> query
/// count (BR-INV-053 — TS-INV-048).
///
/// The concurrency scope is the point: a sale fails only when one of the batches <b>it allocated</b>
/// changed between allocation and commit. A concurrent change to another batch of the same product
/// must not fail it — a wider scope would produce the false-failure storm the owner ruled against.
/// Both directions are asserted here at the persistence level, where the interleaving is
/// deterministic, plus one end-to-end race through the API.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class InventoryConsumptionConcurrencyTests(ApiFixture fixture)
{
    [Fact]
    public async Task A_change_to_an_allocated_batch_between_allocation_and_commit_fails_the_sale_TS_INV_052()
    {
        var product = await SeedProductAsync(batches: [50m]);
        var batchId = (await BatchIdsAsync(product)).Single();

        await using var seller = NewDbContext();
        var writer = new InventoryConsumptionWriter(seller, new FixedClinicClock(), TimeProvider.System);
        var result = await writer.StageAsync(
            [Request(product, 10m)],
            CancellationToken.None);
        result.Succeeded.ShouldBeTrue();

        // Another connection consumes from the SAME batch and commits first.
        await using (var rival = NewDbContext())
        {
            var rivalBatch = await rival.InventoryBatches.SingleAsync(
                batch => batch.Id == batchId, CancellationToken.None);
            rivalBatch.Consume(5m);
            await rival.SaveChangesAsync(CancellationToken.None);
        }

        // The first sale's allocated batch moved under it — detected, never overwritten.
        await Should.ThrowAsync<DbUpdateConcurrencyException>(
            () => seller.SaveChangesAsync(CancellationToken.None));

        // Only the rival's decrement landed: no double deduction, no negative remainder.
        (await RemainingAsync(batchId)).ShouldBe(45m);
    }

    [Fact]
    public async Task A_change_to_a_different_batch_of_the_same_product_does_not_fail_the_sale_TS_INV_052()
    {
        // Two batches; the sale allocates only the first (FEFO), so a concurrent change to the
        // second must not fail it — the check is scoped to the batch, not the product (R6).
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var product = await SeedProductAsync(batches: [50m, 50m], expiries: [today.AddDays(10), today.AddDays(60)]);
        var untouched = await BatchIdWithExpiryAsync(product, today.AddDays(60));

        await using var seller = NewDbContext();
        var writer = new InventoryConsumptionWriter(seller, new FixedClinicClock(), TimeProvider.System);
        (await writer.StageAsync([Request(product, 10m)], CancellationToken.None))
            .Succeeded.ShouldBeTrue();

        await using (var rival = NewDbContext())
        {
            var rivalBatch = await rival.InventoryBatches.SingleAsync(
                batch => batch.Id == untouched, CancellationToken.None);
            rivalBatch.Consume(5m);
            await rival.SaveChangesAsync(CancellationToken.None);
        }

        // No exception: the sale's own batch is untouched.
        await seller.SaveChangesAsync(CancellationToken.None);

        (await RemainingAsync(await BatchIdWithExpiryAsync(product, today.AddDays(10)))).ShouldBe(40m);
        (await RemainingAsync(untouched)).ShouldBe(45m);
    }

    [Fact]
    public async Task Two_racing_commits_on_one_batch_never_double_deduct_TS_INV_052()
    {
        // End to end: stock covers only one of the two sales. Whichever loses is rejected with a
        // business reason — a concurrency conflict or, if it reallocated first, insufficient stock —
        // and the losing invoice stays a draft. What must never happen: two successes, a negative
        // remainder, or a silent overwrite.
        var product = await SeedProductAsync(batches: [10m]);
        var first = await DraftWithLineAsync(product, 10m);
        var second = await DraftWithLineAsync(product, 10m);

        var responses = await Task.WhenAll(CommitAsync(first), CommitAsync(second));

        responses.Count(response => response.IsSuccessStatusCode).ShouldBe(1);
        var loser = responses.Single(response => !response.IsSuccessStatusCode);
        loser.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await ErrorCodeAsync(loser)).ShouldBeOneOf(
            InventoryErrorCodes.ConcurrencyConflict,
            InventoryErrorCodes.InsufficientStock);

        (await RemainingAsync((await BatchIdsAsync(product)).Single())).ShouldBe(0m);
        (await OnHandAsync(product)).ShouldBe(0m);

        var statuses = await fixture.QueryDbAsync(db => db.SalesInvoices
            .Where(invoice => invoice.Id == first || invoice.Id == second)
            .Select(invoice => invoice.Status)
            .ToListAsync());
        statuses.Count(status => status == SalesInvoiceStatus.Committed).ShouldBe(1);
        statuses.Count(status => status == SalesInvoiceStatus.Draft).ShouldBe(1);
    }

    [Fact]
    public async Task Allocation_issues_a_constant_number_of_queries_TS_INV_048()
    {
        // Ten products with several batches each must cost the same number of reads as two: one
        // ordered candidate query plus one on-hand query, whatever the line count (BR-INV-053).
        var few = await SeedManyProductsAsync(count: 2, batchesPerProduct: 3);
        var many = await SeedManyProductsAsync(count: 10, batchesPerProduct: 3);

        var fewReads = await CountReadsWhileStagingAsync(few);
        var manyReads = await CountReadsWhileStagingAsync(many);

        fewReads.ShouldBe(2);
        manyReads.ShouldBe(fewReads);   // constant, not a function of the number of lines or batches
    }

    private async Task<int> CountReadsWhileStagingAsync(IReadOnlyList<Guid> productIds)
    {
        var counter = new CommandCountingInterceptor();
        await using var dbContext = NewDbContext(counter);
        var writer = new InventoryConsumptionWriter(dbContext, new FixedClinicClock(), TimeProvider.System);

        counter.Reset();
        var result = await writer.StageAsync(
            [.. productIds.Select(productId => Request(productId, 1m))],
            CancellationToken.None);
        result.Succeeded.ShouldBeTrue();

        return counter.Count;
    }

    // ---- seeding and helpers ------------------------------------------------------------------

    private async Task<Guid> SeedProductAsync(decimal[] batches, DateOnly[]? expiries = null)
    {
        var marker = Guid.NewGuid().ToString("N")[..8];
        var productId = Guid.Empty;

        await fixture.SeedAsync(dbContext =>
        {
            var category = CatalogSeeder.NewCategory(dbContext, $"تصنيف {marker}");
            var manufacturer = CatalogSeeder.NewManufacturer(dbContext, $"مصنّع {marker}");
            var product = CatalogSeeder.NewProduct(
                dbContext, $"منتج {marker}", category.Id, manufacturer.Id, SeededCatalogIds.MedicineNature,
                isSplittable: true, hasExpiration: expiries is not null, stripPrice: 5m);
            productId = product.Id;

            for (var index = 0; index < batches.Length; index++)
            {
                InventorySeeder.AddBatch(dbContext, productId, batches[index], expiries?[index]);
            }

            InventorySeeder.SetOnHand(dbContext, productId, batches.Sum());
            return Task.CompletedTask;
        });

        return productId;
    }

    private async Task<IReadOnlyList<Guid>> SeedManyProductsAsync(int count, int batchesPerProduct)
    {
        var productIds = new List<Guid>(count);
        for (var index = 0; index < count; index++)
        {
            productIds.Add(await SeedProductAsync([.. Enumerable.Repeat(10m, batchesPerProduct)]));
        }

        return productIds;
    }

    private async Task<Guid> DraftWithLineAsync(Guid productId, decimal quantity)
    {
        var create = await fixture.Client.PostAsJsonAsync(
            new Uri("/api/v1/sales-invoices", UriKind.Relative),
            new { saleDate = new DateOnly(2026, 7, 30) });
        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        using var body = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var invoiceId = body.RootElement.GetProperty("id").GetGuid();

        var line = await fixture.Client.PostAsJsonAsync(
            new Uri($"/api/v1/sales-invoices/{invoiceId}/lines", UriKind.Relative),
            new { productId, saleUnitId = SeededCatalogIds.StripUnit, quantity });
        line.StatusCode.ShouldBe(HttpStatusCode.Created);

        return invoiceId;
    }

    private Task<HttpResponseMessage> CommitAsync(Guid invoiceId) =>
        fixture.Client.PostAsync(new Uri($"/api/v1/sales-invoices/{invoiceId}/commit", UriKind.Relative), content: null);

    private VetFlowDbContext NewDbContext(IInterceptor? interceptor = null)
    {
        var builder = new DbContextOptionsBuilder<VetFlowDbContext>().UseNpgsql(fixture.ConnectionString);
        if (interceptor is not null)
        {
            builder.AddInterceptors(interceptor);
        }

        return new VetFlowDbContext(builder.Options);
    }

    private static InventoryConsumptionRequest Request(Guid productId, decimal quantity) =>
        new() { ProductId = productId, StockQuantity = quantity, SaleLineId = Guid.NewGuid() };

    private Task<List<Guid>> BatchIdsAsync(Guid productId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.ProductId == productId)
            .Select(batch => batch.Id)
            .ToListAsync());

    private Task<Guid> BatchIdWithExpiryAsync(Guid productId, DateOnly expiry) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.ProductId == productId && batch.ExpiryDate == expiry)
            .Select(batch => batch.Id)
            .SingleAsync());

    private Task<decimal> RemainingAsync(Guid batchId) =>
        fixture.QueryDbAsync(dbContext => dbContext.InventoryBatches
            .Where(batch => batch.Id == batchId)
            .Select(batch => batch.RemainingQuantity)
            .SingleAsync());

    private Task<decimal> OnHandAsync(Guid productId) =>
        fixture.QueryDbAsync(dbContext => dbContext.ProductOnHands
            .Where(item => item.ProductId == productId)
            .Select(item => item.OnHandQuantity)
            .SingleAsync());

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return problem.RootElement.GetProperty("errorCode").GetString();
    }

    /// <summary>A clinic clock pinned to a real "today" — the allocator only needs the date.</summary>
    private sealed class FixedClinicClock : IClinicClock
    {
        public DateOnly Today { get; } = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo")).DateTime);
    }

    /// <summary>Counts the SQL commands a block of work actually issues (BR-INV-053).</summary>
    private sealed class CommandCountingInterceptor : DbCommandInterceptor
    {
        public int Count { get; private set; }

        public void Reset() => Count = 0;

        public override InterceptionResult<System.Data.Common.DbDataReader> ReaderExecuting(
            System.Data.Common.DbCommand command,
            CommandEventData eventData,
            InterceptionResult<System.Data.Common.DbDataReader> result)
        {
            Count++;
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
            System.Data.Common.DbCommand command,
            CommandEventData eventData,
            InterceptionResult<System.Data.Common.DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Count++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
