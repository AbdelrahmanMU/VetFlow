using VetFlow.Infrastructure.Persistence;
using VetFlow.Infrastructure.Persistence.Numbering;

namespace VetFlow.IntegrationTests;

/// <summary>
/// Number allocation for the test seeders, through the real allocator rather than around it —
/// so seeded documents carry numbers from the same branch counter as real ones (ADR-0022 §6) and
/// cannot drift from the production format.
///
/// The allocator refuses to run outside a transaction, which is what keeps the series gapless.
/// A seeder that is already inside one (the uncommitted-read helper) joins it; otherwise this
/// opens and commits one of its own.
/// </summary>
public static class TestDocumentNumbers
{
    public static async Task<long> NextAsync(VetFlowDbContext dbContext, DocumentSeries series)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        var allocator = new DocumentNumbers(dbContext, new TestTenantContext());

        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await allocator.NextAsync(series, CancellationToken.None);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        var value = await allocator.NextAsync(series, CancellationToken.None);
        await transaction.CommitAsync();
        return value;
    }
}
