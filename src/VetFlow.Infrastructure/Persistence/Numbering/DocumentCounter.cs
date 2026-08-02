namespace VetFlow.Infrastructure.Persistence.Numbering;

/// <summary>
/// One counter row per (tenant, scope, series) — the structure that replaced the five
/// database-global sequences (ADR-0022 §6, §12.12).
///
/// <b>It is a persistence mechanism, not a business concept</b>, which is why it lives in
/// Infrastructure beside the allocator rather than in a module's domain. No business rule mentions
/// it; what the business ruled is the <i>outcome</i> — every clinic's first invoice is number one,
/// and no number is ever burned.
///
/// It is nevertheless a mapped entity so that it is created by a migration like every other table,
/// declares a tenant scope like every other table, and is covered by the isolation tests that walk
/// the model. A hand-made table outside the model would be invisible to exactly the checks that
/// exist to catch a missed discriminator.
///
/// <b>Rows are never read or written through EF.</b> Allocation is one atomic statement in
/// <see cref="DocumentNumbers"/>; reading the value into memory first would reintroduce the race
/// the counter exists to remove.
/// </summary>
public sealed class DocumentCounter
{
    private DocumentCounter()
    {
        // EF Core materialization only.
        Series = string.Empty;
    }

    /// <summary>
    /// The tenant or the branch this counter belongs to, per <see cref="DocumentSeriesScope"/>.
    /// One column rather than a nullable branch column: a nullable member of a primary key is not
    /// expressible in PostgreSQL, and "the scope that owns this series" is the honest name for it.
    /// </summary>
    public Guid ScopeId { get; private set; }

    /// <summary>The series code — <c>PRD</c>, <c>PUR</c>, <c>PRT</c>, <c>SAL</c>, <c>SRT</c>.</summary>
    public string Series { get; private set; }

    /// <summary>The last number handed out. The next allocation returns this plus one.</summary>
    public long LastValue { get; private set; }
}
